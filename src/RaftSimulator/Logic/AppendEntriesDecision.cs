using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Result of handling an append entries request.
/// </summary>
/// <param name="Response">Append entries response to return.</param>
/// <param name="LogLine">Log line for the decision.</param>
/// <param name="StatusSnapshot">Optional status snapshot to publish.</param>
internal sealed record AppendEntriesDecision(
    RaftAppendEntriesResponse Response,
    string LogLine,
    RaftStatus? StatusSnapshot);
