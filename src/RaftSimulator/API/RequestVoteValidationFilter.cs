using RaftSimulator.Transport;
using RaftSimulator.Contracts;

namespace RaftSimulator.API;

/// <summary>
/// Validates request-vote endpoint payloads.
/// </summary>
internal sealed class RequestVoteValidationFilter : RaftRequestValidationFilter<RaftVoteRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestVoteValidationFilter"/> class.
    /// </summary>
    public RequestVoteValidationFilter()
        : base(RaftDtoValidator.TryValidate)
    {
    }
}
