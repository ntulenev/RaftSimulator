namespace RaftSimulator.Contracts;

/// <summary>
/// Append entries response DTO.
/// </summary>
/// <param name="Term">Responder term.</param>
/// <param name="FromId">Responder node identifier.</param>
/// <param name="Success">Value indicating whether append entries was accepted.</param>
internal sealed record RaftAppendEntriesResponseDto(int Term, int FromId, bool Success);
