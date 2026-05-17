namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when a peer grants a vote.
/// </summary>
internal sealed record VoteResponseGrantedEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoteResponseGrantedEvent"/> class.
    /// </summary>
    /// <param name="fromId">Peer node identifier.</param>
    /// <param name="totalVotes">Total votes received.</param>
    /// <param name="majority">Majority threshold.</param>
    public VoteResponseGrantedEvent(FromId fromId, int totalVotes, int majority)
    {
        ArgumentNullException.ThrowIfNull(fromId);

        FromId = fromId;
        TotalVotes = DomainEventGuard.RequirePositiveCount(totalVotes, nameof(totalVotes), "Total votes");
        Majority = DomainEventGuard.RequirePositiveCount(majority, nameof(majority), "Majority");

        if (totalVotes > majority)
        {
            throw new ArgumentOutOfRangeException(nameof(totalVotes), totalVotes, "Total votes must be <= majority.");
        }
    }

    /// <summary>
    /// Gets peer node identifier.
    /// </summary>
    public FromId FromId { get; }

    /// <summary>
    /// Gets total votes received.
    /// </summary>
    public int TotalVotes { get; }

    /// <summary>
    /// Gets majority threshold.
    /// </summary>
    public int Majority { get; }
}
