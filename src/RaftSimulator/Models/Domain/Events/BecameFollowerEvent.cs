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
    public BecameFollowerEvent(Term term, LeaderId? leaderId)
    {
        ArgumentNullException.ThrowIfNull(term);

        Term = term;
        LeaderId = leaderId;
    }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets known leader identifier.
    /// </summary>
    public LeaderId? LeaderId { get; }
}
