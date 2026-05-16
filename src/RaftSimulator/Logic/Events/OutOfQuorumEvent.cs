namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when the leader cannot reach quorum.
/// </summary>
/// <param name="Reachable">Reachable node count including self.</param>
/// <param name="Total">Total node count.</param>
/// <param name="Needed">Majority threshold.</param>
internal sealed record OutOfQuorumEvent(int Reachable, int Total, int Needed) : RaftEvent;
