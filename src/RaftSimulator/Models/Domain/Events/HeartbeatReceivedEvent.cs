namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when an incoming heartbeat is accepted.
/// </summary>
internal sealed record HeartbeatReceivedEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatReceivedEvent"/> class.
    /// </summary>
    /// <param name="leaderId">Leader node identifier.</param>
    /// <param name="term">Current term.</param>
    public HeartbeatReceivedEvent(int leaderId, int term)
    {
        LeaderId = DomainEventGuard.RequireNodeId(leaderId, nameof(leaderId), "Leader id");
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets leader node identifier.
    /// </summary>
    public int LeaderId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public int Term { get; }
}
