namespace RaftSimulator.Contracts;

/// <summary>
/// Vote response DTO.
/// </summary>
/// <param name="Term">Responder term.</param>
/// <param name="FromId">Responder node identifier.</param>
/// <param name="Granted">Value indicating whether vote was granted.</param>
internal sealed record RaftVoteResponseDto(int Term, int FromId, bool Granted);
