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
