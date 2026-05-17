namespace RaftSimulator.Contracts;

/// <summary>
/// Append entries request DTO.
/// </summary>
/// <param name="Term">Leader term.</param>
/// <param name="LeaderId">Leader node identifier.</param>
internal sealed record RaftAppendEntriesRequestDto(int Term, int LeaderId);
