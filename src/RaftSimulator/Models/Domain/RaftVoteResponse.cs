namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote response from raft peers.
/// </summary>
internal sealed record RaftVoteResponse(int Term, int FromId, bool Granted);
