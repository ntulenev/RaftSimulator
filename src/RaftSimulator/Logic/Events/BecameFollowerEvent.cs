namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when the node becomes follower.
/// </summary>
/// <param name="Term">Current term.</param>
/// <param name="LeaderId">Known leader identifier.</param>
internal sealed record BecameFollowerEvent(int Term, int? LeaderId) : RaftEvent;
