namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote request sent to raft peers.
/// </summary>
/// <param name="Term">Candidate term.</param>
/// <param name="CandidateId">Candidate node identifier.</param>
internal sealed record RaftVoteRequest(Term Term, CandidateId CandidateId);
