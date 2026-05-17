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
    public VoteResponseDeniedEvent(FromId fromId, Term term)
    {
        ArgumentNullException.ThrowIfNull(fromId);
        ArgumentNullException.ThrowIfNull(term);

        FromId = fromId;
        Term = term;
    }

    /// <summary>
    /// Gets peer node identifier.
    /// </summary>
    public FromId FromId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public Term Term { get; }
}
