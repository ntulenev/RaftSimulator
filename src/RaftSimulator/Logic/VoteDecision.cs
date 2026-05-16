using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Result of handling a vote request.
/// </summary>
/// <param name="Response">Vote response to return.</param>
/// <param name="Events">Events emitted by the decision.</param>
internal sealed record VoteDecision(RaftVoteResponse Response, IReadOnlyList<RaftEvent> Events);
