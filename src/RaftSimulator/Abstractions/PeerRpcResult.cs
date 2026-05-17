using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Result of a raft RPC call to a peer.
/// </summary>
/// <typeparam name="TResponse">Response payload type.</typeparam>
/// <param name="Peer">Target peer.</param>
/// <param name="Response">Response payload, when available.</param>
/// <param name="Error">Transport error, when the call failed unexpectedly.</param>
internal sealed record PeerRpcResult<TResponse>(
    PeerInfo Peer,
    TResponse? Response,
    Exception? Error)
    where TResponse : class
{
    /// <summary>
    /// Creates a successful peer RPC result.
    /// </summary>
    /// <param name="peer">Target peer.</param>
    /// <param name="response">Response payload.</param>
    /// <returns>Peer RPC result.</returns>
    public static PeerRpcResult<TResponse> Success(PeerInfo peer, TResponse response) =>
        new(peer, response, null);

    /// <summary>
    /// Creates a peer RPC result for an unavailable peer.
    /// </summary>
    /// <param name="peer">Target peer.</param>
    /// <returns>Peer RPC result.</returns>
    public static PeerRpcResult<TResponse> Unavailable(PeerInfo peer) =>
        new(peer, default, null);

    /// <summary>
    /// Creates a failed peer RPC result.
    /// </summary>
    /// <param name="peer">Target peer.</param>
    /// <param name="error">Transport error.</param>
    /// <returns>Peer RPC result.</returns>
    public static PeerRpcResult<TResponse> Failed(PeerInfo peer, Exception error) =>
        new(peer, default, error);
}
