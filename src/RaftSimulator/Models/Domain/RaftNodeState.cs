namespace RaftSimulator.Models.Domain;

/// <summary>
/// Mutable state owned by a raft node state machine.
/// </summary>
internal sealed class RaftNodeState
{
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
        ScheduleHeartbeat(now, heartbeatInterval);
    }

    internal void StartElection(int nodeId, DateTimeOffset now, TimeSpan electionTimeout)
    {
        Role = RaftRole.Candidate;
        CurrentTerm = CurrentTerm.Next();
        VotedFor = new CandidateId(nodeId);
        VotesReceived = 1;
        LeaderId = null;
        ScheduleElection(now, electionTimeout);
    }

    internal void BecomeLeader(int nodeId, DateTimeOffset now)
    {
        Role = RaftRole.Leader;
        LeaderId = new LeaderId(nodeId);
        VotesReceived = 0;
        NextHeartbeatAt = now;
        LeaderSince = now;
        LastHeartbeatAckAt.Clear();
    }

    internal void BecomeFollower(
        int term,
        int? leaderId,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
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

    internal void ScheduleElection(DateTimeOffset now, TimeSpan electionTimeout) =>
        NextElectionDeadline = now + electionTimeout;

    internal void ScheduleHeartbeat(DateTimeOffset now, TimeSpan heartbeatInterval) =>
        NextHeartbeatAt = now + heartbeatInterval;

    internal void RegisterHeartbeatAck(int peerId, DateTimeOffset now) =>
        LastHeartbeatAckAt[peerId] = now;

    private void ResetLeaderTracking()
    {
        LeaderSince = default;
        LastHeartbeatAckAt.Clear();
    }

    /// <summary>
    /// Gets or sets current node role.
    /// </summary>
    public RaftRole Role { get; set; }

    /// <summary>
    /// Gets or sets current term.
    /// </summary>
    public Term CurrentTerm { get; set; } = Term.Initial;

    /// <summary>
    /// Gets or sets node voted for in the current term.
    /// </summary>
    public CandidateId? VotedFor { get; set; }

    /// <summary>
    /// Gets or sets known leader identifier.
    /// </summary>
    public LeaderId? LeaderId { get; set; }

    /// <summary>
    /// Gets or sets votes received in the current election.
    /// </summary>
    public int VotesReceived { get; set; }

    /// <summary>
    /// Gets or sets time when this node became leader.
    /// </summary>
    public DateTimeOffset LeaderSince { get; set; }

    /// <summary>
    /// Gets or sets next election deadline.
    /// </summary>
    public DateTimeOffset NextElectionDeadline { get; set; }

    /// <summary>
    /// Gets or sets next heartbeat deadline.
    /// </summary>
    public DateTimeOffset NextHeartbeatAt { get; set; }

    /// <summary>
    /// Gets last successful heartbeat acknowledgement time by peer identifier.
    /// </summary>
    public Dictionary<int, DateTimeOffset> LastHeartbeatAckAt { get; } = [];
}
