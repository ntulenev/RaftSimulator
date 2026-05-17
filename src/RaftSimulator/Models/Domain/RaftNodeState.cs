namespace RaftSimulator.Models.Domain;

/// <summary>
/// Mutable state owned by a raft node state machine.
/// </summary>
internal sealed class RaftNodeState
{
    private readonly Dictionary<int, DateTimeOffset> _lastHeartbeatAckAt = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RaftNodeState"/> class.
    /// </summary>
    public RaftNodeState()
    {
    }

    internal void InitializeFollower(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval)
    {
        Role = RaftRole.Follower;
        CurrentTerm = Term.Initial;
        VotedFor = null;
        LeaderId = null;
        VotesReceived = 0;
        ResetLeaderTracking();
        ScheduleElection(now, electionTimeout);
        NextHeartbeatAt = now + heartbeatInterval;
    }

    internal void StartElection(int nodeId, DateTimeOffset now, TimeSpan electionTimeout)
    {
        if (Role == RaftRole.Leader)
        {
            throw new InvalidOperationException("Leader cannot start a new election.");
        }

        Role = RaftRole.Candidate;
        CurrentTerm = CurrentTerm.Next();
        VotedFor = new CandidateId(nodeId);
        VotesReceived = 1;
        LeaderId = null;
        ScheduleElection(now, electionTimeout);
    }

    internal void BecomeLeader(int nodeId, int majority, DateTimeOffset now)
    {
        if (Role != RaftRole.Candidate)
        {
            throw new InvalidOperationException("Only candidate can become leader.");
        }

        if (VotesReceived < majority)
        {
            throw new InvalidOperationException("Candidate cannot become leader before receiving majority.");
        }

        Role = RaftRole.Leader;
        LeaderId = new LeaderId(nodeId);
        VotesReceived = 0;
        NextHeartbeatAt = now;
        LeaderSince = now;
        _lastHeartbeatAckAt.Clear();
    }

    internal void BecomeFollower(
        int term,
        int? leaderId,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        if (term < CurrentTerm.Value)
        {
            throw new InvalidOperationException("Node cannot move to an older term.");
        }

        Role = RaftRole.Follower;
        LeaderId = leaderId is null ? null : new LeaderId(leaderId.Value);
        VotesReceived = 0;
        ResetLeaderTracking();

        if (term > CurrentTerm.Value)
        {
            CurrentTerm = new Term(term);
            VotedFor = null;
        }

        ScheduleElection(now, electionTimeout);
    }

    internal bool TryGrantVote(
        RaftVoteRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsStaleFor(CurrentTerm))
        {
            throw new InvalidOperationException("Cannot grant a stale vote request.");
        }

        if (!request.CanBeGrantedBy(Role, VotedFor))
        {
            return false;
        }

        VotedFor = request.CandidateId;
        ScheduleElection(now, electionTimeout);
        return true;
    }

    internal void AcceptHeartbeat(
        RaftAppendEntriesRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsStaleFor(CurrentTerm))
        {
            throw new InvalidOperationException("Cannot accept a stale heartbeat.");
        }

        if (Role != RaftRole.Follower)
        {
            throw new InvalidOperationException("Only follower can accept a leader heartbeat.");
        }

        LeaderId = request.LeaderId;
        ScheduleElection(now, electionTimeout);
    }

    internal int RecordGrantedVote()
    {
        if (Role != RaftRole.Candidate)
        {
            throw new InvalidOperationException("Only candidate can record granted votes.");
        }

        VotesReceived++;
        return VotesReceived;
    }

    internal bool HasMajority(int majority) => VotesReceived >= majority;

    internal void ScheduleElection(DateTimeOffset now, TimeSpan electionTimeout) =>
        NextElectionDeadline = now + electionTimeout;

    internal void ScheduleHeartbeat(DateTimeOffset now, TimeSpan heartbeatInterval)
    {
        if (Role != RaftRole.Leader)
        {
            throw new InvalidOperationException("Only leader can schedule heartbeats.");
        }

        NextHeartbeatAt = now + heartbeatInterval;
    }

    internal void RegisterHeartbeatAck(int peerId, DateTimeOffset now)
    {
        if (Role != RaftRole.Leader)
        {
            throw new InvalidOperationException("Only leader can register heartbeat acknowledgements.");
        }

        _lastHeartbeatAckAt[peerId] = now;
    }

    private void ResetLeaderTracking()
    {
        LeaderSince = default;
        _lastHeartbeatAckAt.Clear();
    }

    /// <summary>
    /// Gets current node role.
    /// </summary>
    public RaftRole Role { get; private set; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public Term CurrentTerm { get; private set; } = Term.Initial;

    /// <summary>
    /// Gets node voted for in the current term.
    /// </summary>
    public CandidateId? VotedFor { get; private set; }

    /// <summary>
    /// Gets known leader identifier.
    /// </summary>
    public LeaderId? LeaderId { get; private set; }

    /// <summary>
    /// Gets votes received in the current election.
    /// </summary>
    public int VotesReceived { get; private set; }

    /// <summary>
    /// Gets time when this node became leader.
    /// </summary>
    public DateTimeOffset LeaderSince { get; private set; }

    /// <summary>
    /// Gets next election deadline.
    /// </summary>
    public DateTimeOffset NextElectionDeadline { get; private set; }

    /// <summary>
    /// Gets next heartbeat deadline.
    /// </summary>
    public DateTimeOffset NextHeartbeatAt { get; private set; }

    /// <summary>
    /// Gets last successful heartbeat acknowledgement time by peer identifier.
    /// </summary>
    public IReadOnlyDictionary<int, DateTimeOffset> LastHeartbeatAckAt => _lastHeartbeatAckAt;
}
