using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Result of handling a vote request.
/// </summary>
/// <param name="Response">Vote response to return.</param>
/// <param name="LogLine">Log line for the decision.</param>
internal sealed record VoteDecision(RaftVoteResponse Response, string LogLine);
