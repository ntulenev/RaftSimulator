namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote response from raft peers.
/// </summary>
internal sealed record RaftVoteResponse(Term Term, FromId FromId, bool Granted);
