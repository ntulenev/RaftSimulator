namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries response from raft peers.
/// </summary>
internal sealed record RaftAppendEntriesResponse(int Term, int FromId, bool Success);
