namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when the node becomes leader.
/// </summary>
/// <param name="Term">Leader term.</param>
internal sealed record BecameLeaderEvent(int Term) : RaftEvent;
