namespace RaftSimulator.API;

/// <summary>
/// Validates a request DTO.
/// </summary>
/// <typeparam name="TRequest">Request DTO type.</typeparam>
/// <param name="request">Request DTO.</param>
/// <param name="error">Validation error, when invalid.</param>
/// <returns>True when the request is valid.</returns>
internal delegate bool TryValidateRequest<in TRequest>(TRequest request, out string? error);
