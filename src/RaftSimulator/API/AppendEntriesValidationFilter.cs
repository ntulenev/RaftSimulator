using RaftSimulator.Transport;
using RaftSimulator.Contracts;

namespace RaftSimulator.API;

/// <summary>
/// Validates append-entries endpoint payloads.
/// </summary>
internal sealed class AppendEntriesValidationFilter : IEndpointFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendEntriesValidationFilter"/> class.
    /// </summary>
    public AppendEntriesValidationFilter()
    {
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.GetArgument<RaftAppendEntriesRequestDto>(0);
        return RaftDtoValidator.TryValidate(request, out var error)
            ? next(context)
            : ValueTask.FromResult<object?>(Results.BadRequest(error));
    }
}
