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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Heartbeat run result.</returns>
    Task<HeartbeatRunResult> SendHeartbeatsAsync(
        int term,
        int nodeId,
        CancellationToken cancellationToken);
}
