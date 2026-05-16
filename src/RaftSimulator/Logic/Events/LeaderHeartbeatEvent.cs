namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when leader heartbeat timeout elapses.
/// </summary>
internal sealed record LeaderHeartbeatEvent : RaftEvent;
