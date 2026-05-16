using System.Threading.Channels;

using RaftSimulator.Abstractions;
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
    /// <param name="clock">Clock.</param>
    /// <param name="random">Random source.</param>
    public RaftNode(
        RaftSettings settings,
        IRaftPeerBroadcaster peerBroadcaster,
        IRaftLog log,
        IRaftClock clock,
        IRaftRandom random)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(peerBroadcaster);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(random);

        _settings = settings;
        _peerBroadcaster = peerBroadcaster;
        _log = log;
        _clock = clock;
        _random = random;
        _stateMachine = new RaftStateMachine(settings);
        _scheduleSignal = Channel.CreateUnbounded<bool>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
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
                var delay = GetNextDelay();
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }

                var delayTask = Task.Delay(delay, cancellationToken);
                var signalTask = _scheduleSignal.Reader.ReadAsync(cancellationToken).AsTask();
                var completed = await Task
                    .WhenAny(delayTask, signalTask)
                    .ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (completed == signalTask)
                {
                    DrainScheduleSignals();
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

        _log.WriteNode(Id, decision.LogLine);
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

        _log.WriteNode(Id, decision.LogLine);
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
        _log.WriteNode(Id, action.LogLine);

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
            if (decision.LogLines.Count > 0 || decision.BecameLeader)
            {
                SignalScheduleChangeUnsafe();
            }
        }

        foreach (var logLine in decision.LogLines)
        {
            _log.WriteNode(Id, logLine);
        }

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

    private void DrainScheduleSignals()
    {
        while (_scheduleSignal.Reader.TryRead(out _))
        {
        }
    }

    private void SignalScheduleChangeUnsafe() =>
        _ = _scheduleSignal.Writer.TryWrite(true);

    private void RegisterHeartbeatAck(int peerId)
    {
        lock (_gate)
        {
            _stateMachine.RegisterHeartbeatAck(peerId, _clock.UtcNow);
        }
    }

    private void ReportQuorum()
    {
        string? warning;

        lock (_gate)
        {
            warning = _stateMachine.BuildQuorumWarning(_clock.UtcNow, GetQuorumWindow());
        }

        if (warning is not null)
        {
            _log.WriteNode(Id, warning);
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
            if (decision.LogLines.Count > 0)
            {
                SignalScheduleChangeUnsafe();
            }
        }

        foreach (var logLine in decision.LogLines)
        {
            _log.WriteNode(Id, logLine);
        }
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

    private readonly RaftSettings _settings;
    private readonly IRaftPeerBroadcaster _peerBroadcaster;
    private readonly IRaftLog _log;
    private readonly IRaftClock _clock;
    private readonly IRaftRandom _random;
    private readonly RaftStateMachine _stateMachine;
    private readonly Channel<bool> _scheduleSignal;
    private readonly Lock _gate = new();
}
