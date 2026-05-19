using RaftSimulator.Models.Domain.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Pure raft election state transitions for a single node.
/// </summary>
internal sealed class RaftStateMachine
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
    /// Handles a request-vote RPC.
    /// </summary>
    /// <param name="request">Vote request.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Vote decision.</returns>
    public VoteDecision HandleRequestVote(
        RaftVoteRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        var events = new List<RaftEvent>(2);

        if (request.IsStaleFor(State.CurrentTerm))
        {
            events.Add(CreateVoteEvent(request, granted: false, request.Term));
            return CreateVoteDecision(granted: false, events);
        }

        if (request.AdvancesTerm(State.CurrentTerm))
        {
            events.Add(BecomeFollower(request.Term.Value, null, now, electionTimeout));
        }

        var canVote = State.TryGrantVote(request, now, electionTimeout);
        events.Add(CreateVoteEvent(request, canVote, State.CurrentTerm));

        return CreateVoteDecision(canVote, events);
    }

    /// <summary>
    /// Handles an append-entries RPC.
    /// </summary>
    /// <param name="request">Append entries request.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Append entries decision.</returns>
    public AppendEntriesDecision HandleAppendEntries(
        RaftAppendEntriesRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        var events = new List<RaftEvent>(2);

        if (request.IsStaleFor(State.CurrentTerm))
        {
            events.Add(new HeartbeatIgnoredEvent(request.LeaderId, request.Term));
            return CreateAppendEntriesDecision(success: false, events, statusSnapshot: null);
        }

        if (request.ShouldMakeFollower(State.CurrentTerm, State.Role))
        {
            events.Add(BecomeFollower(
                request.Term.Value,
                request.LeaderId.Value,
                now,
                electionTimeout));
        }

        State.AcceptHeartbeat(request, now, electionTimeout);

        events.Add(new HeartbeatReceivedEvent(request.LeaderId, State.CurrentTerm));

        return CreateAppendEntriesDecision(
            success: true,
            events,
            _statusReporter.GetSnapshotToPublish(GetStatus()));
    }

    /// <summary>
    /// Prepares work for the next elapsed timeout.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <param name="heartbeatInterval">Heartbeat interval.</param>
    /// <returns>Timeout action.</returns>
    public TimeoutAction PrepareTimeoutAction(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval)
    {
        if (State.Role == RaftRole.Leader)
        {
            State.ScheduleHeartbeat(now, heartbeatInterval);
            return new TimeoutAction(
                TimeoutActionType.Heartbeats,
                State.CurrentTerm,
                [new LeaderHeartbeatEvent()]);
        }

        State.StartElection(Id, now, electionTimeout);

        return new TimeoutAction(
            TimeoutActionType.Election,
            State.CurrentTerm,
            [new ElectionTimeoutEvent(State.CurrentTerm)]);
    }

    /// <summary>
    /// Handles a vote response from a peer.
    /// </summary>
    /// <param name="response">Vote response.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Vote response decision.</returns>
    public VoteResponseDecision HandleVoteResponse(
        RaftVoteResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(response);

        var events = new List<RaftEvent>(2);
        var becameLeader = false;
        var term = response.Term;
        RaftStatus? statusSnapshot = null;

        if (response.HasHigherTermThan(State.CurrentTerm))
        {
            events.Add(new HigherTermDiscoveredEvent(response.Term, response.FromId));
            events.Add(BecomeFollower(response.Term.Value, null, now, electionTimeout));
        }
        else if (State.Role != RaftRole.Candidate || !response.IsForTerm(State.CurrentTerm))
        {
            return CreateVoteResponseDecision([], becameLeader: false, term, statusSnapshot: null);
        }
        else if (!response.Granted)
        {
            events.Add(new VoteResponseDeniedEvent(response.FromId, State.CurrentTerm));
        }
        else
        {
            var votesReceived = State.RecordGrantedVote();
            events.Add(new VoteResponseGrantedEvent(
                response.FromId,
                votesReceived,
                _settings.Majority));

            if (State.HasMajority(_settings.Majority))
            {
                State.BecomeLeader(Id, _settings.Majority, now);
                events.Add(new BecameLeaderEvent(State.CurrentTerm));
                becameLeader = true;
                term = State.CurrentTerm;

                statusSnapshot = _statusReporter.GetSnapshotToPublish(GetStatus());
            }
        }

        return CreateVoteResponseDecision(events, becameLeader, term, statusSnapshot);
    }

    /// <summary>
    /// Handles an append-entries response from a peer.
    /// </summary>
    /// <param name="response">Append entries response.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Append entries response decision.</returns>
    public AppendEntriesResponseDecision HandleAppendEntriesResponse(
        RaftAppendEntriesResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.HasHigherTermThan(State.CurrentTerm))
        {
            return new AppendEntriesResponseDecision([]);
        }

        return new AppendEntriesResponseDecision(
        [
            new HigherTermDiscoveredEvent(response.Term, response.FromId),
            BecomeFollower(response.Term.Value, null, now, electionTimeout)
        ]);
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
            State.LastHeartbeatAckAt,
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
