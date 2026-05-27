namespace RaftSimulator.API;

/// <summary>
/// Base filter for validating raft endpoint payloads.
/// </summary>
/// <typeparam name="TRequest">Request DTO type.</typeparam>
internal abstract class RaftRequestValidationFilter<TRequest> : IEndpointFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftRequestValidationFilter{TRequest}"/> class.
    /// </summary>
    /// <param name="validator">Request validator.</param>
    protected RaftRequestValidationFilter(TryValidateRequest<TRequest> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.GetArgument<TRequest>(0);
        return _validator(request, out var error)
            ? next(context)
            : ValueTask.FromResult<object?>(Results.BadRequest(error));
    }

    private readonly TryValidateRequest<TRequest> _validator;
}
