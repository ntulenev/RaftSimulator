namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when a peer denies a vote.
/// </summary>
/// <param name="FromId">Peer node identifier.</param>
/// <param name="Term">Current term.</param>
internal sealed record VoteResponseDeniedEvent(int FromId, int Term) : RaftEvent;
