using RaftSimulator.Transport;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.API;

/// <summary>
/// Validates request-vote endpoint payloads.
/// </summary>
internal sealed class RequestVoteValidationFilter : IEndpointFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestVoteValidationFilter"/> class.
    /// </summary>
    public RequestVoteValidationFilter()
    {
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.GetArgument<RaftVoteRequestDto>(0);
        return RaftDtoValidator.TryValidate(request, out var error)
            ? next(context)
            : ValueTask.FromResult<object?>(Results.BadRequest(error));
    }
}
