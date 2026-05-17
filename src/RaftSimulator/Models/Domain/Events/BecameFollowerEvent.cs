namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when the node becomes follower.
/// </summary>
internal sealed record BecameFollowerEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BecameFollowerEvent"/> class.
    /// </summary>
    /// <param name="term">Current term.</param>
    /// <param name="leaderId">Known leader identifier.</param>
    public BecameFollowerEvent(int term, int? leaderId)
    {
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
        LeaderId = leaderId is null
            ? null
            : DomainEventGuard.RequireNodeId(leaderId.Value, nameof(leaderId), "Leader id");
    }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public int Term { get; }

    /// <summary>
    /// Gets known leader identifier.
    /// </summary>
    public int? LeaderId { get; }
}
