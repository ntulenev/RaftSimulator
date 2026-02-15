namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote request sent to raft peers.
/// </summary>
internal sealed record RaftVoteRequest(int Term, int CandidateId);
