namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries response from raft peers.
/// </summary>
internal sealed record RaftAppendEntriesResponse(Term Term, FromId FromId, bool Success);
