namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when the leader cannot reach quorum.
/// </summary>
internal sealed record OutOfQuorumEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutOfQuorumEvent"/> class.
    /// </summary>
    /// <param name="reachable">Reachable node count including self.</param>
    /// <param name="total">Total node count.</param>
    /// <param name="needed">Majority threshold.</param>
    public OutOfQuorumEvent(int reachable, int total, int needed)
    {
        Reachable = DomainEventGuard.RequirePositiveCount(reachable, nameof(reachable), "Reachable nodes");
        Total = DomainEventGuard.RequirePositiveCount(total, nameof(total), "Total nodes");
        Needed = DomainEventGuard.RequirePositiveCount(needed, nameof(needed), "Needed nodes");

        if (reachable > total)
        {
            throw new ArgumentOutOfRangeException(nameof(reachable), reachable, "Reachable nodes must be <= total nodes.");
        }

        if (needed > total)
        {
            throw new ArgumentOutOfRangeException(nameof(needed), needed, "Needed nodes must be <= total nodes.");
        }

        if (reachable >= needed)
        {
            throw new ArgumentOutOfRangeException(nameof(reachable), reachable, "Reachable nodes must be less than needed nodes.");
        }
    }

    /// <summary>
    /// Gets reachable node count including self.
    /// </summary>
    public int Reachable { get; }

    /// <summary>
    /// Gets total node count.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Gets majority threshold.
    /// </summary>
    public int Needed { get; }
}
