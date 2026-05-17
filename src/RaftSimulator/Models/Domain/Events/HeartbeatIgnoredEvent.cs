namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when an incoming heartbeat is ignored.
/// </summary>
internal sealed record HeartbeatIgnoredEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatIgnoredEvent"/> class.
    /// </summary>
    /// <param name="leaderId">Leader node identifier from the request.</param>
    /// <param name="term">Request term.</param>
    public HeartbeatIgnoredEvent(int leaderId, int term)
    {
        LeaderId = DomainEventGuard.RequireNodeId(leaderId, nameof(leaderId), "Leader id");
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets leader node identifier from the request.
    /// </summary>
    public int LeaderId { get; }

    /// <summary>
    /// Gets request term.
    /// </summary>
    public int Term { get; }
}
