using RaftSimulator.Logic.Events;
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
        State.Role = RaftRole.Follower;
        State.CurrentTerm = 0;
        State.VotedFor = null;
        State.LeaderId = null;
        State.VotesReceived = 0;
        State.LeaderSince = default;
        State.LastHeartbeatAckAt.Clear();
        State.LastReportedTerm = -1;
        State.NextElectionDeadline = now + electionTimeout;
        State.NextHeartbeatAt = now + heartbeatInterval;
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

        RaftVoteResponse response;
        var events = new List<RaftEvent>(2);

        if (request.Term.Value < State.CurrentTerm)
        {
            response = new RaftVoteResponse(State.CurrentTerm, Id, false);
            events.Add(new RequestVoteDeniedEvent(request.CandidateId.Value, request.Term.Value));
        }
        else
        {
            if (request.Term.Value > State.CurrentTerm)
            {
                events.Add(BecomeFollower(request.Term.Value, null, now, electionTimeout));
            }

            var canVote = State.Role != RaftRole.Leader
                && (State.VotedFor is null || State.VotedFor == request.CandidateId);
            if (canVote)
            {
                State.VotedFor = request.CandidateId.Value;
                State.NextElectionDeadline = now + electionTimeout;
            }

            response = new RaftVoteResponse(State.CurrentTerm, Id, canVote);
            events.Add(canVote
                ? new RequestVoteGrantedEvent(request.CandidateId.Value, State.CurrentTerm)
                : new RequestVoteDeniedEvent(request.CandidateId.Value, State.CurrentTerm));
        }

        return new VoteDecision(response, events);
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

        RaftAppendEntriesResponse response;
        var events = new List<RaftEvent>(2);
        RaftStatus? statusSnapshot = null;

        if (request.Term.Value < State.CurrentTerm)
        {
            response = new RaftAppendEntriesResponse(State.CurrentTerm, Id, false);
            events.Add(new HeartbeatIgnoredEvent(request.LeaderId.Value, request.Term.Value));
        }
        else
        {
            if (request.Term.Value > State.CurrentTerm || State.Role != RaftRole.Follower)
            {
                events.Add(BecomeFollower(
                    request.Term.Value,
                    request.LeaderId.Value,
                    now,
                    electionTimeout));
            }

            State.LeaderId = request.LeaderId.Value;
            State.NextElectionDeadline = now + electionTimeout;

            response = new RaftAppendEntriesResponse(State.CurrentTerm, Id, true);
            events.Add(new HeartbeatReceivedEvent(request.LeaderId.Value, State.CurrentTerm));

            if (TryGetElectionStatusSnapshot(out var snapshot))
            {
                statusSnapshot = snapshot;
            }
        }

        return new AppendEntriesDecision(response, events, statusSnapshot);
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
            State.NextHeartbeatAt = now + heartbeatInterval;
            return new TimeoutAction(
                TimeoutActionType.Heartbeats,
                State.CurrentTerm,
                [new LeaderHeartbeatEvent()]);
        }

        State.Role = RaftRole.Candidate;
        State.CurrentTerm++;
        State.VotedFor = Id;
        State.VotesReceived = 1;
        State.LeaderId = null;
        State.NextElectionDeadline = now + electionTimeout;

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
        var term = response.Term.Value;
        RaftStatus? statusSnapshot = null;

        if (response.Term.Value > State.CurrentTerm)
        {
            events.Add(new HigherTermDiscoveredEvent(response.Term.Value, response.FromId.Value));
            events.Add(BecomeFollower(response.Term.Value, null, now, electionTimeout));
        }
        else if (State.Role != RaftRole.Candidate || response.Term.Value != State.CurrentTerm)
        {
            return new VoteResponseDecision([], false, term, null);
        }
        else if (!response.Granted)
        {
            events.Add(new VoteResponseDeniedEvent(response.FromId.Value, State.CurrentTerm));
        }
        else
        {
            State.VotesReceived++;
            events.Add(new VoteResponseGrantedEvent(
                response.FromId.Value,
                State.VotesReceived,
                _settings.Majority));

            if (State.VotesReceived >= _settings.Majority)
            {
                State.Role = RaftRole.Leader;
                State.LeaderId = Id;
                State.VotesReceived = 0;
                State.NextHeartbeatAt = now;
                State.LeaderSince = now;
                State.LastHeartbeatAckAt.Clear();
                events.Add(new BecameLeaderEvent(State.CurrentTerm));
                becameLeader = true;
                term = State.CurrentTerm;

                if (TryGetElectionStatusSnapshot(out var snapshot))
                {
                    statusSnapshot = snapshot;
                }
            }
        }

        return new VoteResponseDecision(events, becameLeader, term, statusSnapshot);
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

        if (response.Term.Value <= State.CurrentTerm)
        {
            return new AppendEntriesResponseDecision([]);
        }

        var events = new RaftEvent[]
        {
            new HigherTermDiscoveredEvent(response.Term.Value, response.FromId.Value),
            BecomeFollower(response.Term.Value, null, now, electionTimeout)
        };

        return new AppendEntriesResponseDecision(events);
    }

    /// <summary>
    /// Registers a successful heartbeat acknowledgement.
    /// </summary>
    /// <param name="peerId">Peer node identifier.</param>
    /// <param name="now">Current time.</param>
    public void RegisterHeartbeatAck(int peerId, DateTimeOffset now) =>
        State.LastHeartbeatAckAt[peerId] = now;

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

        var cutoff = now - window;
        var reachablePeers = 0;

        foreach (var peer in _settings.Peers)
        {
            if (State.LastHeartbeatAckAt.TryGetValue(peer.Id, out var ackAt) && ackAt >= cutoff)
            {
                reachablePeers++;
            }
        }

        var reachable = reachablePeers + 1;
        var needed = _settings.Majority;
        var total = _settings.NodeCount;

        return reachable >= needed
            ? null
            : new OutOfQuorumEvent(reachable, total, needed);
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
        new(Id, State.CurrentTerm, State.Role, State.LeaderId);

    private BecameFollowerEvent BecomeFollower(
        int term,
        int? leaderId,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        State.Role = RaftRole.Follower;
        State.LeaderId = leaderId;
        State.VotesReceived = 0;
        State.LeaderSince = default;
        State.LastHeartbeatAckAt.Clear();

        if (term > State.CurrentTerm)
        {
            State.CurrentTerm = term;
            State.VotedFor = null;
        }

        State.NextElectionDeadline = now + electionTimeout;

        return new BecameFollowerEvent(State.CurrentTerm, leaderId);
    }

    private bool TryGetElectionStatusSnapshot(out RaftStatus snapshot)
    {
        if (State.LeaderId is null || State.CurrentTerm <= State.LastReportedTerm)
        {
            snapshot = default!;
            return false;
        }

        State.LastReportedTerm = State.CurrentTerm;
        snapshot = new RaftStatus(Id, State.CurrentTerm, State.Role, State.LeaderId);
        return true;
    }

    private readonly RaftSettings _settings;

    private RaftNodeState State { get; } = new();
}
