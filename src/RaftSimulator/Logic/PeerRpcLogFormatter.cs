namespace RaftSimulator.Logic;

/// <summary>
/// Formats peer RPC log messages.
/// </summary>
internal static class PeerRpcLogFormatter
{
    /// <summary>
    /// Formats a peer RPC failure.
    /// </summary>
    /// <param name="rpcName">RPC name.</param>
    /// <param name="peerId">Peer identifier.</param>
    /// <param name="term">RPC term.</param>
    /// <param name="exception">Failure exception.</param>
    /// <returns>Formatted failure message.</returns>
    public static string FormatFailure(
        string rpcName,
        int peerId,
        int term,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(rpcName);
        ArgumentNullException.ThrowIfNull(exception);

        return exception is HttpRequestException or TaskCanceledException
            ? $"Unable to reach Node {peerId:00}."
            : $"{rpcName} (term {term}) -> Node {peerId:00} failed: " +
              $"{exception.GetType().Name}: {exception.Message}";
    }
}
