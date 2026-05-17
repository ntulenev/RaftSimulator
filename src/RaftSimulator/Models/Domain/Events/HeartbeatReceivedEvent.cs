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
    public HeartbeatReceivedEvent(LeaderId leaderId, Term term)
    {
        ArgumentNullException.ThrowIfNull(leaderId);
        ArgumentNullException.ThrowIfNull(term);

        LeaderId = leaderId;
        Term = term;
    }

    /// <summary>
    /// Gets leader node identifier.
    /// </summary>
    public LeaderId LeaderId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public Term Term { get; }
}
