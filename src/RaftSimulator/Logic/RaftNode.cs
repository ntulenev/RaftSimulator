using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

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
        _coordinator = new RaftNodeCoordinator(settings, clock, delayProvider, runtime);
        _runtime = runtime;
        _electionRunner = electionRunner;
        _heartbeatRunner = heartbeatRunner;
    }

    /// <inheritdoc />
    public int Id => _settings.NodeId;

    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken) =>
        _runtime.RunAsync(
            Id,
            _coordinator.InitializeState,
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

        var decision = _coordinator.HandleRequestVote(request);

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

        var decision = _coordinator.HandleAppendEntries(request);

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
        return _coordinator.GetStatus();
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
        return _coordinator.PrepareTimeoutAction();
    }

    private async Task HandleVoteResponseAsync(
        RaftVoteResponse response,
        CancellationToken cancellationToken)
    {
        var decision = _coordinator.HandleVoteResponse(response);

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
        return _coordinator.GetNextDelay();
    }

    private void RegisterHeartbeatAck(int peerId)
    {
        _coordinator.RegisterHeartbeatAck(new FromId(peerId));
    }

    private void ReportQuorum()
    {
        RaftEvent? quorumEvent;

        quorumEvent = _coordinator.BuildQuorumEvent(GetQuorumWindow());

        if (quorumEvent is not null)
        {
            LogEvent(quorumEvent);
        }
    }

    private TimeSpan GetQuorumWindow() =>
        _settings.HeartbeatInterval + _settings.MaxNetworkDelay;

    private void HandleAppendEntriesResponse(RaftAppendEntriesResponse response)
    {
        var decision = _coordinator.HandleAppendEntriesResponse(response);

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
    private readonly IRaftNodeRuntime _runtime;
    private readonly RaftNodeCoordinator _coordinator;
    private readonly IRaftElectionRunner _electionRunner;
    private readonly IRaftHeartbeatRunner _heartbeatRunner;
}
