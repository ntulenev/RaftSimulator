using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain.Events;
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
    /// <param name="log">Log sink.</param>
    /// <param name="eventLog">Event log sink.</param>
    /// <param name="clock">Clock.</param>
    /// <param name="delayProvider">Delay provider.</param>
    /// <param name="runtime">Runtime loop.</param>
    /// <param name="electionRunner">Election runner.</param>
    /// <param name="heartbeatRunner">Heartbeat runner.</param>
    public RaftNode(
        RaftSettings settings,
        IRaftLog log,
        IRaftEventLog eventLog,
        IRaftClock clock,
        IRaftDelayProvider delayProvider,
        IRaftNodeRuntime runtime,
        IRaftElectionRunner electionRunner,
        IRaftHeartbeatRunner heartbeatRunner)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(electionRunner);
        ArgumentNullException.ThrowIfNull(heartbeatRunner);

        _settings = settings;
        _log = log;
        _eventLog = eventLog;
        _clock = clock;
        _delayProvider = delayProvider;
        _runtime = runtime;
        _electionRunner = electionRunner;
        _heartbeatRunner = heartbeatRunner;
        _stateMachine = new RaftStateMachine(settings);
    }

    /// <inheritdoc />
    public int Id => _settings.NodeId;

    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken) =>
        _runtime.RunAsync(
            Id,
            InitializeState,
            GetNextDelay,
            HandleTimeoutAsync,
            cancellationToken);

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

        await _electionRunner
            .StartElectionAsync(action.Term, new CandidateId(Id), HandleVoteResponseAsync, cancellationToken)
            .ConfigureAwait(false);
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

    private async Task SendHeartbeatsAsync(Term term, CancellationToken cancellationToken)
    {
        ReportQuorum();

        var result = await _heartbeatRunner
            .SendHeartbeatsAsync(term, new LeaderId(Id), cancellationToken)
            .ConfigureAwait(false);

        foreach (var response in result.Responses)
        {
            HandleAppendEntriesResponse(response);
        }

        foreach (var peerId in result.AcknowledgedPeerIds)
        {
            RegisterHeartbeatAck(peerId);
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
        _delayProvider.GetDelay(_settings.MinElectionTimeout, _settings.MaxElectionTimeout);

    private void SignalScheduleChangeUnsafe() =>
        _runtime.Signal();

    private void RegisterHeartbeatAck(int peerId)
    {
        lock (_gate)
        {
            _stateMachine.RegisterHeartbeatAck(new FromId(peerId), _clock.UtcNow);
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
    private readonly IRaftLog _log;
    private readonly IRaftEventLog _eventLog;
    private readonly IRaftClock _clock;
    private readonly IRaftDelayProvider _delayProvider;
    private readonly IRaftNodeRuntime _runtime;
    private readonly RaftStateMachine _stateMachine;
    private readonly IRaftElectionRunner _electionRunner;
    private readonly IRaftHeartbeatRunner _heartbeatRunner;
    private readonly Lock _gate = new();
}
