using RaftSimulator.Models.Domain;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.Transport;

/// <summary>
/// Maps between domain models and transport DTOs.
/// </summary>
internal static class RaftDtoMapper
{
    public static RaftVoteRequest ToDomain(RaftVoteRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftVoteRequest(dto.Term, dto.CandidateId);
    }

    public static RaftVoteRequestDto ToDto(RaftVoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RaftVoteRequestDto(request.Term, request.CandidateId);
    }

    public static RaftVoteResponseDto ToDto(RaftVoteResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new RaftVoteResponseDto(response.Term, response.FromId, response.Granted);
    }

    public static RaftVoteResponse ToDomain(RaftVoteResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftVoteResponse(dto.Term, dto.FromId, dto.Granted);
    }

    public static RaftAppendEntriesRequest ToDomain(RaftAppendEntriesRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftAppendEntriesRequest(dto.Term, dto.LeaderId);
    }

    public static RaftAppendEntriesRequestDto ToDto(RaftAppendEntriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RaftAppendEntriesRequestDto(request.Term, request.LeaderId);
    }

    public static RaftAppendEntriesResponseDto ToDto(RaftAppendEntriesResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new RaftAppendEntriesResponseDto(response.Term, response.FromId, response.Success);
    }

    public static RaftAppendEntriesResponse ToDomain(RaftAppendEntriesResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RaftAppendEntriesResponse(dto.Term, dto.FromId, dto.Success);
    }

    public static RaftStatusDto ToDto(RaftStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new RaftStatusDto(status.NodeId, status.Term, status.Role.ToString(), status.LeaderId);
    }
}
