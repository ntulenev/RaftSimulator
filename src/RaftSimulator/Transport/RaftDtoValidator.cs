using RaftSimulator.Transport.Models;

namespace RaftSimulator.Transport;

/// <summary>
/// Validates raft transport DTOs before mapping them into domain models.
/// </summary>
internal static class RaftDtoValidator
{
    /// <summary>
    /// Validates a request-vote DTO.
    /// </summary>
    /// <param name="request">Request DTO.</param>
    /// <param name="error">Validation error, when invalid.</param>
    /// <returns>True when the DTO is valid.</returns>
    public static bool TryValidate(RaftVoteRequestDto request, out string? error)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Term < 1)
        {
            error = "Term must be positive.";
            return false;
        }

        if (request.CandidateId < 1)
        {
            error = "CandidateId must be positive.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Validates an append-entries DTO.
    /// </summary>
    /// <param name="request">Request DTO.</param>
    /// <param name="error">Validation error, when invalid.</param>
    /// <returns>True when the DTO is valid.</returns>
    public static bool TryValidate(RaftAppendEntriesRequestDto request, out string? error)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Term < 1)
        {
            error = "Term must be positive.";
            return false;
        }

        if (request.LeaderId < 1)
        {
            error = "LeaderId must be positive.";
            return false;
        }

        error = null;
        return true;
    }
}
