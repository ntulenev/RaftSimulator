using RaftSimulator.Logic.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of handling a vote request.
/// </summary>
/// <param name="Response">Vote response to return.</param>
/// <param name="Events">Events emitted by the decision.</param>
internal sealed record VoteDecision(RaftVoteResponse Response, IReadOnlyList<RaftEvent> Events);
