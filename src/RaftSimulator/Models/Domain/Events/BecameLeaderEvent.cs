namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when the node becomes leader.
/// </summary>
internal sealed record BecameLeaderEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BecameLeaderEvent"/> class.
    /// </summary>
    /// <param name="term">Leader term.</param>
    public BecameLeaderEvent(int term)
    {
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets leader term.
    /// </summary>
    public int Term { get; }
}
