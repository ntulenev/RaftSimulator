namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when an incoming heartbeat is ignored.
/// </summary>
/// <param name="LeaderId">Leader node identifier from the request.</param>
/// <param name="Term">Request term.</param>
internal sealed record HeartbeatIgnoredEvent(int LeaderId, int Term) : RaftEvent;
