using RaftSimulator.Models.Domain.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Pure raft election state transitions for a single node.
/// </summary>
internal sealed partial class RaftStateMachine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftStateMachine"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    public RaftStateMachine(RaftSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    /// <summary>
    /// Gets local node identifier.
    /// </summary>
    public int Id => _settings.NodeId;

    /// <summary>
    /// Initializes state as a follower.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Initial election timeout.</param>
    /// <param name="heartbeatInterval">Heartbeat interval.</param>
    public void Initialize(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval)
    {
        State.InitializeFollower(now, electionTimeout, heartbeatInterval);
        _statusReporter.Reset();
    }

    /// <summary>
    /// Registers a successful heartbeat acknowledgement.
    /// </summary>
    /// <param name="peerId">Peer node identifier.</param>
    /// <param name="now">Current time.</param>
    public void RegisterHeartbeatAck(FromId peerId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        State.RegisterHeartbeatAck(peerId.Value, now);
    }

    /// <summary>
    /// Builds an out-of-quorum event when the current leader cannot reach majority.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="window">Quorum freshness window.</param>
    /// <returns>Out-of-quorum event, or null when quorum is available or not yet reportable.</returns>
    public RaftEvent? BuildQuorumEvent(DateTimeOffset now, TimeSpan window)
    {
        if (State.LeaderSince == default || now - State.LeaderSince < window)
        {
            return null;
        }

        return RaftQuorumEvaluator.BuildOutOfQuorumEvent(
            _settings.Peers,
            State.GetLastHeartbeatAckSnapshot(),
            _settings.Majority,
            _settings.NodeCount,
            now,
            window);
    }

    /// <summary>
    /// Gets the delay until the next scheduled state-machine action.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <returns>Delay until the next action.</returns>
    public TimeSpan GetNextDelay(DateTimeOffset now)
    {
        var deadline = State.Role == RaftRole.Leader
            ? State.NextHeartbeatAt
            : State.NextElectionDeadline;
        return deadline - now;
    }

    /// <summary>
    /// Gets a snapshot of current node status.
    /// </summary>
    /// <returns>Status snapshot.</returns>
    public RaftStatus GetStatus() =>
        new(new NodeId(Id), State.CurrentTerm, State.Role, State.LeaderId);

    private VoteDecision CreateVoteDecision(bool granted, IReadOnlyList<RaftEvent> events) =>
        new(new RaftVoteResponse(State.CurrentTerm, new FromId(Id), granted), events);

    private static RaftEvent CreateVoteEvent(
        RaftVoteRequest request,
        bool granted,
        Term term) =>
        granted
            ? new RequestVoteGrantedEvent(request.CandidateId, term)
            : new RequestVoteDeniedEvent(request.CandidateId, term);

    private AppendEntriesDecision CreateAppendEntriesDecision(
        bool success,
        IReadOnlyList<RaftEvent> events,
        RaftStatus? statusSnapshot) =>
        new(new RaftAppendEntriesResponse(State.CurrentTerm, new FromId(Id), success), events, statusSnapshot);

    private static VoteResponseDecision CreateVoteResponseDecision(
        IReadOnlyList<RaftEvent> events,
        bool becameLeader,
        Term term,
        RaftStatus? statusSnapshot) =>
        new(events, becameLeader, term, statusSnapshot);

    private BecameFollowerEvent BecomeFollower(
        int term,
        int? leaderId,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        State.BecomeFollower(term, leaderId, now, electionTimeout);
        return new BecameFollowerEvent(
            State.CurrentTerm,
            leaderId is null ? null : new LeaderId(leaderId.Value));
    }

    private readonly RaftSettings _settings;
    private readonly RaftStatusReporter _statusReporter = new();

    private RaftNodeState State { get; } = new();
}
