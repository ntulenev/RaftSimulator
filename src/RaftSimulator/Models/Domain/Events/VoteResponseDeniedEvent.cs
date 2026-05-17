namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when a peer denies a vote.
/// </summary>
internal sealed record VoteResponseDeniedEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoteResponseDeniedEvent"/> class.
    /// </summary>
    /// <param name="fromId">Peer node identifier.</param>
    /// <param name="term">Current term.</param>
    public VoteResponseDeniedEvent(int fromId, int term)
    {
        FromId = DomainEventGuard.RequireNodeId(fromId, nameof(fromId), "Peer id");
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets peer node identifier.
    /// </summary>
    public int FromId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public int Term { get; }
}
