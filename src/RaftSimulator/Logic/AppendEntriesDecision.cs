using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Result of handling an append entries request.
/// </summary>
/// <param name="Response">Append entries response to return.</param>
/// <param name="Events">Events emitted by the decision.</param>
/// <param name="StatusSnapshot">Optional status snapshot to publish.</param>
internal sealed record AppendEntriesDecision(
    RaftAppendEntriesResponse Response,
    IReadOnlyList<RaftEvent> Events,
    RaftStatus? StatusSnapshot);
