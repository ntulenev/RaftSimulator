namespace RaftSimulator.Contracts;

/// <summary>
/// Vote request DTO.
/// </summary>
/// <param name="Term">Candidate term.</param>
/// <param name="CandidateId">Candidate node identifier.</param>
internal sealed record RaftVoteRequestDto(int Term, int CandidateId);
