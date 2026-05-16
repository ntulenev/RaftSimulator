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
        string logLine;

        if (request.Term < State.CurrentTerm)
        {
            response = new RaftVoteResponse(State.CurrentTerm, Id, false);
            logLine = $"Denied vote to Node {request.CandidateId:00} (term {request.Term}).";
        }
        else
        {
            logLine = request.Term > State.CurrentTerm
                ? BecomeFollower(request.Term, null, now, electionTimeout)
                : string.Empty;

            var canVote = State.Role != RaftRole.Leader
                && (State.VotedFor is null || State.VotedFor == request.CandidateId);
            if (canVote)
            {
                State.VotedFor = request.CandidateId;
                State.NextElectionDeadline = now + electionTimeout;
            }

            response = new RaftVoteResponse(State.CurrentTerm, Id, canVote);
            logLine = string.IsNullOrWhiteSpace(logLine)
                ? $"{(canVote ? "Granted" : "Denied")} vote to Node {request.CandidateId:00} " +
                  $"(term {State.CurrentTerm})."
                : logLine + $" {(canVote ? "Granted" : "Denied")} vote to Node " +
                  $"{request.CandidateId:00} (term {State.CurrentTerm}).";
        }

        return new VoteDecision(response, logLine);
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
        string logLine;
        RaftStatus? statusSnapshot = null;

        if (request.Term < State.CurrentTerm)
        {
            response = new RaftAppendEntriesResponse(State.CurrentTerm, Id, false);
            logLine = $"Ignored heartbeat from Node {request.LeaderId:00} (term {request.Term}).";
        }
        else
        {
            logLine = request.Term > State.CurrentTerm || State.Role != RaftRole.Follower
                ? BecomeFollower(request.Term, request.LeaderId, now, electionTimeout)
                : string.Empty;

            State.LeaderId = request.LeaderId;
            State.NextElectionDeadline = now + electionTimeout;

            response = new RaftAppendEntriesResponse(State.CurrentTerm, Id, true);
            logLine = string.IsNullOrWhiteSpace(logLine)
                ? $"Heartbeat from Node {request.LeaderId:00} (term {State.CurrentTerm})."
                : logLine + $" Heartbeat from Node {request.LeaderId:00} (term {State.CurrentTerm}).";

            if (TryGetElectionStatusSnapshot(out var snapshot))
            {
                statusSnapshot = snapshot;
            }
        }

        return new AppendEntriesDecision(response, logLine, statusSnapshot);
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
                "Leader heartbeat.");
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
            $"Election timeout. Term {State.CurrentTerm}, becoming candidate.");
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

        var logs = new List<string>(2);
        var becameLeader = false;
        var term = response.Term;
        RaftStatus? statusSnapshot = null;

        if (response.Term > State.CurrentTerm)
        {
            logs.Add($"Discovered higher term {response.Term} from Node {response.FromId:00}.");
            logs.Add(BecomeFollower(response.Term, null, now, electionTimeout));
        }
        else if (State.Role != RaftRole.Candidate || response.Term != State.CurrentTerm)
        {
            return new VoteResponseDecision([], false, term, null);
        }
        else if (!response.Granted)
        {
            logs.Add($"Vote denied by Node {response.FromId:00} (term {State.CurrentTerm}).");
        }
        else
        {
            State.VotesReceived++;
            logs.Add(
                $"Vote granted by Node {response.FromId:00}. Total={State.VotesReceived}/{_settings.Majority}.");

            if (State.VotesReceived >= _settings.Majority)
            {
                State.Role = RaftRole.Leader;
                State.LeaderId = Id;
                State.VotesReceived = 0;
                State.NextHeartbeatAt = now;
                State.LeaderSince = now;
                State.LastHeartbeatAckAt.Clear();
                logs.Add($"Became leader for term {State.CurrentTerm}.");
                becameLeader = true;
                term = State.CurrentTerm;

                if (TryGetElectionStatusSnapshot(out var snapshot))
                {
                    statusSnapshot = snapshot;
                }
            }
        }

        return new VoteResponseDecision(logs, becameLeader, term, statusSnapshot);
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

        if (response.Term <= State.CurrentTerm)
        {
            return new AppendEntriesResponseDecision([]);
        }

        var logs = new[]
        {
            $"Discovered higher term {response.Term} from Node {response.FromId:00}.",
            BecomeFollower(response.Term, null, now, electionTimeout)
        };

        return new AppendEntriesResponseDecision(logs);
    }

    /// <summary>
    /// Registers a successful heartbeat acknowledgement.
    /// </summary>
    /// <param name="peerId">Peer node identifier.</param>
    /// <param name="now">Current time.</param>
    public void RegisterHeartbeatAck(int peerId, DateTimeOffset now) =>
        State.LastHeartbeatAckAt[peerId] = now;

    /// <summary>
    /// Builds an out-of-quorum warning when the current leader cannot reach majority.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="window">Quorum freshness window.</param>
    /// <returns>Warning message, or null when quorum is available or not yet reportable.</returns>
    public string? BuildQuorumWarning(DateTimeOffset now, TimeSpan window)
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
            : $"Cluster out of quorum: {reachable}/{total} (need {needed}).";
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

    private string BecomeFollower(
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

        var leaderText = leaderId is null ? "unknown" : $"Node {leaderId:00}";
        return $"Became follower for term {State.CurrentTerm} (leader {leaderText}).";
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
