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
    public HigherTermDiscoveredEvent(Term term, FromId fromId)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(fromId);

        Term = term;
        FromId = fromId;
    }

    /// <summary>
    /// Gets higher term.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets peer node identifier.
    /// </summary>
    public FromId FromId { get; }
}
