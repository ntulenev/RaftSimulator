using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Transport;

/// <summary>
/// Creates peer RPC result objects from transport outcomes.
/// </summary>
internal static class PeerRpcResultFactory
{
    /// <summary>
    /// Creates a result from an optional response payload.
    /// </summary>
    /// <typeparam name="TResponse">Response type.</typeparam>
    /// <param name="peer">Peer information.</param>
    /// <param name="response">Response payload.</param>
    /// <returns>Peer RPC result.</returns>
    public static PeerRpcResult<TResponse> FromResponse<TResponse>(
        PeerInfo peer,
        TResponse? response)
        where TResponse : class =>
        response is null
            ? PeerRpcResult<TResponse>.Unavailable(peer)
            : PeerRpcResult<TResponse>.Success(peer, response);

    /// <summary>
    /// Creates a result from a caught transport exception.
    /// </summary>
    /// <typeparam name="TResponse">Response type.</typeparam>
    /// <param name="peer">Peer information.</param>
    /// <param name="exception">Caught exception.</param>
    /// <returns>Peer RPC result.</returns>
    public static PeerRpcResult<TResponse> FromException<TResponse>(
        PeerInfo peer,
        Exception exception)
        where TResponse : class =>
        PeerRpcResult<TResponse>.Failed(peer, exception);
}
