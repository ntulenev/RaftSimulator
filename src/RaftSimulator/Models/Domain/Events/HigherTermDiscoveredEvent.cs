namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when a peer response carries a higher term.
/// </summary>
internal sealed record HigherTermDiscoveredEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HigherTermDiscoveredEvent"/> class.
    /// </summary>
    /// <param name="term">Higher term.</param>
    /// <param name="fromId">Peer node identifier.</param>
    public HigherTermDiscoveredEvent(int term, int fromId)
    {
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
        FromId = DomainEventGuard.RequireNodeId(fromId, nameof(fromId), "Peer id");
    }

    /// <summary>
    /// Gets higher term.
    /// </summary>
    public int Term { get; }

    /// <summary>
    /// Gets peer node identifier.
    /// </summary>
    public int FromId { get; }
}
