namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when election timeout elapses.
/// </summary>
internal sealed record ElectionTimeoutEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElectionTimeoutEvent"/> class.
    /// </summary>
    /// <param name="term">New election term.</param>
    public ElectionTimeoutEvent(int term)
    {
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets new election term.
    /// </summary>
    public int Term { get; }
}
