namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote response from raft peers.
/// </summary>
/// <param name="Term">Responder term.</param>
/// <param name="FromId">Responder node identifier.</param>
/// <param name="Granted">Value indicating whether the vote was granted.</param>
internal sealed record RaftVoteResponse(Term Term, FromId FromId, bool Granted);
