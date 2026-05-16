using RaftSimulator.Abstractions;
using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Raft node runtime loop and side-effect orchestration.
/// </summary>
internal sealed class RaftNode : IRaftNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftNode"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    /// <param name="peerBroadcaster">Peer broadcaster.</param>
    /// <param name="log">Log sink.</param>
    /// <param name="eventLog">Event log sink.</param>
    /// <param name="clock">Clock.</param>
    /// <param name="random">Random source.</param>
    /// <param name="scheduler">Runtime scheduler.</param>
    public RaftNode(
        RaftSettings settings,
        IRaftPeerBroadcaster peerBroadcaster,
        IRaftLog log,
        IRaftEventLog eventLog,
        IRaftClock clock,
        IRaftRandom random,
        IRaftScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(peerBroadcaster);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(scheduler);

        _settings = settings;
        _peerBroadcaster = peerBroadcaster;
        _log = log;
        _eventLog = eventLog;
        _clock = clock;
        _random = random;
        _scheduler = scheduler;
        _stateMachine = new RaftStateMachine(settings);
    }

    /// <inheritdoc />
    public int Id => _settings.NodeId;

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        InitializeState();
        _log.WriteNode(Id, "Started as follower.");

        try
        {
            while (true)
            {
                var waitResult = await _scheduler
                    .WaitAsync(GetNextDelay, cancellationToken)
                    .ConfigureAwait(false);

                if (waitResult == RaftScheduleWaitResult.Signaled)
                {
                    continue;
                }

                await HandleTimeoutAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _log.WriteNode(Id, "Stopped.");
        }
    }

    /// <inheritdoc />
    public Task<RaftVoteResponse> OnRequestVoteAsync(
        RaftVoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        VoteDecision decision;

        lock (_gate)
        {
            decision = _stateMachine.HandleRequestVote(
                request,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            SignalScheduleChangeUnsafe();
        }

        LogEvents(decision.Events);
        return Task.FromResult(decision.Response);
    }

    /// <inheritdoc />
    public Task<RaftAppendEntriesResponse> OnAppendEntriesAsync(
        RaftAppendEntriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        AppendEntriesDecision decision;

        lock (_gate)
        {
            decision = _stateMachine.HandleAppendEntries(
                request,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            SignalScheduleChangeUnsafe();
        }

        LogEvents(decision.Events);
        if (decision.StatusSnapshot is not null)
        {
            _log.WriteNodeStatus(decision.StatusSnapshot);
        }

        return Task.FromResult(decision.Response);
    }

    /// <inheritdoc />
    public RaftStatus GetStatus()
    {
        lock (_gate)
        {
            return _stateMachine.GetStatus();
        }
    }

    private void InitializeState()
    {
        lock (_gate)
        {
            _stateMachine.Initialize(
                _clock.UtcNow,
                GetRandomElectionTimeout(),
                _settings.HeartbeatInterval);
            SignalScheduleChangeUnsafe();
        }
    }

    private async Task HandleTimeoutAsync(CancellationToken cancellationToken)
    {
        if (GetNextDelay() > TimeSpan.Zero)
        {
            return;
        }

        var action = PrepareTimeoutAction();
        LogEvents(action.Events);

        if (action.Type == TimeoutActionType.Heartbeats)
        {
            await SendHeartbeatsAsync(action.Term, cancellationToken).ConfigureAwait(false);
            return;
        }

        await StartElectionAsync(action.Term, cancellationToken).ConfigureAwait(false);
    }

    private TimeoutAction PrepareTimeoutAction()
    {
        lock (_gate)
        {
            var action = _stateMachine.PrepareTimeoutAction(
                _clock.UtcNow,
                GetRandomElectionTimeout(),
                _settings.HeartbeatInterval);
            SignalScheduleChangeUnsafe();
            return action;
        }
    }

    private async Task StartElectionAsync(int term, CancellationToken cancellationToken)
    {
        var results = await _peerBroadcaster
            .RequestVotesAsync(term, Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var result in results)
        {
            _log.WriteNode(Id, $"RequestVote -> Node {result.Peer.Id:00} (term {term}).");

            if (result.Error is not null)
            {
                LogPeerFailure("RequestVote", result.Peer.Id, term, result.Error);
                continue;
            }

            if (result.Response is null)
            {
                _log.WriteNode(Id, $"VoteResponse unavailable from Node {result.Peer.Id:00}.");
                continue;
            }

            await HandleVoteResponseAsync(result.Response, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleVoteResponseAsync(
        RaftVoteResponse response,
        CancellationToken cancellationToken)
    {
        VoteResponseDecision decision;

        lock (_gate)
        {
            decision = _stateMachine.HandleVoteResponse(
                response,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            if (decision.Events.Count > 0 || decision.BecameLeader)
            {
                SignalScheduleChangeUnsafe();
            }
        }

        LogEvents(decision.Events);

        if (decision.StatusSnapshot is not null)
        {
            _log.WriteNodeStatus(decision.StatusSnapshot);
        }

        if (decision.BecameLeader)
        {
            await SendHeartbeatsAsync(decision.Term, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendHeartbeatsAsync(int term, CancellationToken cancellationToken)
    {
        ReportQuorum();

        var results = await _peerBroadcaster
            .SendHeartbeatsAsync(term, Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var result in results)
        {
            _log.WriteNode(Id, $"AppendEntries -> Node {result.Peer.Id:00} (term {term}).");

            if (result.Error is not null)
            {
                LogPeerFailure("AppendEntries", result.Peer.Id, term, result.Error);
                continue;
            }

            if (result.Response is null)
            {
                _log.WriteNode(Id, $"AppendEntries unavailable from Node {result.Peer.Id:00}.");
                continue;
            }

            HandleAppendEntriesResponse(result.Response);
            RegisterHeartbeatAck(result.Peer.Id);
        }
    }

    private TimeSpan GetNextDelay()
    {
        lock (_gate)
        {
            return _stateMachine.GetNextDelay(_clock.UtcNow);
        }
    }

    private TimeSpan GetRandomElectionTimeout() =>
        GetRandomDelay(_settings.MinElectionTimeout, _settings.MaxElectionTimeout);

    private TimeSpan GetRandomDelay(TimeSpan min, TimeSpan max)
    {
        if (min == max)
        {
            return min;
        }

        var window = max - min;
        var offset = TimeSpan.FromMilliseconds(window.TotalMilliseconds * _random.NextDouble());
        return min + offset;
    }

    private void SignalScheduleChangeUnsafe() =>
        _scheduler.Signal();

    private void RegisterHeartbeatAck(int peerId)
    {
        lock (_gate)
        {
            _stateMachine.RegisterHeartbeatAck(peerId, _clock.UtcNow);
        }
    }

    private void ReportQuorum()
    {
        RaftEvent? quorumEvent;

        lock (_gate)
        {
            quorumEvent = _stateMachine.BuildQuorumEvent(_clock.UtcNow, GetQuorumWindow());
        }

        if (quorumEvent is not null)
        {
            LogEvent(quorumEvent);
        }
    }

    private TimeSpan GetQuorumWindow() =>
        _settings.HeartbeatInterval + _settings.MaxNetworkDelay;

    private void HandleAppendEntriesResponse(RaftAppendEntriesResponse response)
    {
        AppendEntriesResponseDecision decision;

        lock (_gate)
        {
            decision = _stateMachine.HandleAppendEntriesResponse(
                response,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            if (decision.Events.Count > 0)
            {
                SignalScheduleChangeUnsafe();
            }
        }

        LogEvents(decision.Events);
    }

    private void LogPeerFailure(string rpcName, int peerId, int term, Exception exception)
    {
        if (exception is HttpRequestException or TaskCanceledException)
        {
            _log.WriteNode(Id, $"Unable to reach Node {peerId:00}.");
            return;
        }

        _log.WriteNode(
            Id,
            $"{rpcName} (term {term}) -> Node {peerId:00} failed: " +
            $"{exception.GetType().Name}: {exception.Message}");
    }

    private void LogEvents(IEnumerable<RaftEvent> events)
    {
        foreach (var raftEvent in events)
        {
            LogEvent(raftEvent);
        }
    }

    private void LogEvent(RaftEvent raftEvent) =>
        _eventLog.WriteNodeEvent(Id, raftEvent);

    private readonly RaftSettings _settings;
    private readonly IRaftPeerBroadcaster _peerBroadcaster;
    private readonly IRaftLog _log;
    private readonly IRaftEventLog _eventLog;
    private readonly IRaftClock _clock;
    private readonly IRaftRandom _random;
    private readonly IRaftScheduler _scheduler;
    private readonly RaftStateMachine _stateMachine;
    private readonly Lock _gate = new();
}
