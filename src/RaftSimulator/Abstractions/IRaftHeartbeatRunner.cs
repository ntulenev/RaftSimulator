using RaftSimulator.Models.Domain;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Runs append-entries heartbeat RPC orchestration for a raft node.
/// </summary>
internal interface IRaftHeartbeatRunner
{
    /// <summary>
    /// Sends heartbeat RPC calls and handles received append-entries responses.
    /// </summary>
    /// <param name="term">Leader term.</param>
    /// <param name="nodeId">Local node identifier.</param>
    /// <param name="reportQuorum">Quorum reporting callback.</param>
    /// <param name="handleAppendEntriesResponse">Append-entries response handler.</param>
    /// <param name="registerHeartbeatAck">Heartbeat acknowledgement callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendHeartbeatsAsync(
        int term,
        int nodeId,
        Action reportQuorum,
        Action<RaftAppendEntriesResponse> handleAppendEntriesResponse,
        Action<int> registerHeartbeatAck,
        CancellationToken cancellationToken);
}
