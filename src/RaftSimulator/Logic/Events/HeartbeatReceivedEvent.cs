namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when an incoming heartbeat is accepted.
/// </summary>
/// <param name="LeaderId">Leader node identifier.</param>
/// <param name="Term">Current term.</param>
internal sealed record HeartbeatReceivedEvent(int LeaderId, int Term) : RaftEvent;
