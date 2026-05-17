using RaftSimulator.Logic.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of handling an append entries response from a peer.
/// </summary>
/// <param name="Events">Events emitted by the decision.</param>
internal sealed record AppendEntriesResponseDecision(IReadOnlyList<RaftEvent> Events);
