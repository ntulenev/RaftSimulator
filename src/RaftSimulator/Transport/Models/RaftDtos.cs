namespace RaftSimulator.Transport.Models;

/// <summary>
/// Vote request DTO.
/// </summary>
internal sealed record RaftVoteRequestDto(int Term, int CandidateId);

/// <summary>
/// Vote response DTO.
/// </summary>
internal sealed record RaftVoteResponseDto(int Term, int FromId, bool Granted);

/// <summary>
/// Append entries request DTO.
/// </summary>
internal sealed record RaftAppendEntriesRequestDto(int Term, int LeaderId);

/// <summary>
/// Append entries response DTO.
/// </summary>
internal sealed record RaftAppendEntriesResponseDto(int Term, int FromId, bool Success);

/// <summary>
/// Status DTO.
/// </summary>
internal sealed record RaftStatusDto(int NodeId, int Term, string Role, int? LeaderId);
