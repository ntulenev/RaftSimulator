using RaftSimulator.Models.Domain;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.Transport;

/// <summary>
/// Maps between domain models and transport DTOs.
/// </summary>
internal static class RaftDtoMapper
{
    /// <summary>
    /// Maps a vote request DTO to a domain model.
    /// </summary>
    /// <param name="dto">Vote request DTO.</param>
    /// <returns>Vote request domain model.</returns>
    public static RaftVoteRequest ToDomain(RaftVoteRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftVoteRequest(new Term(dto.Term), new CandidateId(dto.CandidateId));
    }

    /// <summary>
    /// Maps a vote request domain model to a DTO.
    /// </summary>
    /// <param name="request">Vote request domain model.</param>
    /// <returns>Vote request DTO.</returns>
    public static RaftVoteRequestDto ToDto(RaftVoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RaftVoteRequestDto(request.Term.Value, request.CandidateId.Value);
    }

    /// <summary>
    /// Maps a vote response domain model to a DTO.
    /// </summary>
    /// <param name="response">Vote response domain model.</param>
    /// <returns>Vote response DTO.</returns>
    public static RaftVoteResponseDto ToDto(RaftVoteResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new RaftVoteResponseDto(response.Term.Value, response.FromId.Value, response.Granted);
    }

    /// <summary>
    /// Maps a vote response DTO to a domain model.
    /// </summary>
    /// <param name="dto">Vote response DTO.</param>
    /// <returns>Vote response domain model.</returns>
    public static RaftVoteResponse ToDomain(RaftVoteResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftVoteResponse(new Term(dto.Term), new FromId(dto.FromId), dto.Granted);
    }

    /// <summary>
    /// Maps an append entries request DTO to a domain model.
    /// </summary>
    /// <param name="dto">Append entries request DTO.</param>
    /// <returns>Append entries request domain model.</returns>
    public static RaftAppendEntriesRequest ToDomain(RaftAppendEntriesRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftAppendEntriesRequest(new Term(dto.Term), new LeaderId(dto.LeaderId));
    }

    /// <summary>
    /// Maps an append entries request domain model to a DTO.
    /// </summary>
    /// <param name="request">Append entries request domain model.</param>
    /// <returns>Append entries request DTO.</returns>
    public static RaftAppendEntriesRequestDto ToDto(RaftAppendEntriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RaftAppendEntriesRequestDto(request.Term.Value, request.LeaderId.Value);
    }

    /// <summary>
    /// Maps an append entries response domain model to a DTO.
    /// </summary>
    /// <param name="response">Append entries response domain model.</param>
    /// <returns>Append entries response DTO.</returns>
    public static RaftAppendEntriesResponseDto ToDto(RaftAppendEntriesResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new RaftAppendEntriesResponseDto(
            response.Term.Value,
            response.FromId.Value,
            response.Success);
    }

    /// <summary>
    /// Maps an append entries response DTO to a domain model.
    /// </summary>
    /// <param name="dto">Append entries response DTO.</param>
    /// <returns>Append entries response domain model.</returns>
    public static RaftAppendEntriesResponse ToDomain(RaftAppendEntriesResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftAppendEntriesResponse(new Term(dto.Term), new FromId(dto.FromId), dto.Success);
    }

    /// <summary>
    /// Maps a status domain model to a DTO.
    /// </summary>
    /// <param name="status">Status domain model.</param>
    /// <returns>Status DTO.</returns>
    public static RaftStatusDto ToDto(RaftStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new RaftStatusDto(
            status.NodeId.Value,
            status.Term.Value,
            status.Role.ToString(),
            status.LeaderId?.Value);
    }
}
