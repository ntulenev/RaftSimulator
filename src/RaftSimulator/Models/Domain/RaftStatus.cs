namespace RaftSimulator.Models.Domain;

/// <summary>
/// Snapshot of raft node state.
/// </summary>
internal sealed record RaftStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftStatus"/> class.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <param name="term">Current term.</param>
    /// <param name="role">Current role.</param>
    /// <param name="leaderId">Known leader identifier.</param>
    public RaftStatus(NodeId nodeId, Term term, RaftRole role, LeaderId? leaderId)
    {
        ArgumentNullException.ThrowIfNull(nodeId);
        ArgumentNullException.ThrowIfNull(term);

        NodeId = nodeId;
        Term = term;
        Role = role;
        LeaderId = leaderId;
    }

    /// <summary>
    /// Gets node identifier.
    /// </summary>
    public NodeId NodeId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets current role.
    /// </summary>
    public RaftRole Role { get; }

    /// <summary>
    /// Gets known leader identifier.
    /// </summary>
    public LeaderId? LeaderId { get; }

    /// <summary>
    /// Gets a value indicating whether the node currently knows a leader.
    /// </summary>
    public bool HasKnownLeader => LeaderId is not null;
}
