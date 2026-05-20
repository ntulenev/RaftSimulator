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
    /// <param name="runtime">Runtime loop.</param>
    /// <param name="coordinator">Node state coordinator.</param>
    /// <param name="electionRunner">Election runner.</param>
    /// <param name="heartbeatRunner">Heartbeat runner.</param>
    /// <param name="publisher">Decision publisher.</param>
    /// <param name="quorumReporter">Quorum reporter.</param>
    public RaftNode(
        RaftSettings settings,
        IRaftNodeRuntime runtime,
        RaftNodeCoordinator coordinator,
        IRaftElectionRunner electionRunner,
        IRaftHeartbeatRunner heartbeatRunner,
        RaftDecisionPublisher publisher,
        RaftQuorumReporter quorumReporter)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(electionRunner);
        ArgumentNullException.ThrowIfNull(heartbeatRunner);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(quorumReporter);

        _settings = settings;
        _runtime = runtime;
        _coordinator = coordinator;
        _electionRunner = electionRunner;
        _heartbeatRunner = heartbeatRunner;
        _publisher = publisher;
        _quorumReporter = quorumReporter;
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

        _publisher.Publish(decision);
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

        _publisher.Publish(decision);

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
        _publisher.Publish(action);

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

        _publisher.Publish(decision);

        if (decision.BecameLeader)
        {
            await SendHeartbeatsAsync(decision.Term, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendHeartbeatsAsync(Term term, CancellationToken cancellationToken)
    {
        _quorumReporter.Report();

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

    private void HandleAppendEntriesResponse(RaftAppendEntriesResponse response)
    {
        var decision = _coordinator.HandleAppendEntriesResponse(response);

        _publisher.Publish(decision);
    }

    private readonly RaftSettings _settings;
    private readonly IRaftNodeRuntime _runtime;
    private readonly RaftNodeCoordinator _coordinator;
    private readonly IRaftElectionRunner _electionRunner;
    private readonly IRaftHeartbeatRunner _heartbeatRunner;
    private readonly RaftDecisionPublisher _publisher;
    private readonly RaftQuorumReporter _quorumReporter;
}
