using RaftSimulator.Models.Domain;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Runs request-vote RPC orchestration for a raft node.
/// </summary>
internal interface IRaftElectionRunner
{
    /// <summary>
    /// Starts an election and handles received vote responses.
    /// </summary>
    /// <param name="term">Election term.</param>
    /// <param name="nodeId">Local node identifier.</param>
    /// <param name="handleVoteResponseAsync">Vote response handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartElectionAsync(
        int term,
        int nodeId,
        Func<RaftVoteResponse, CancellationToken, Task> handleVoteResponseAsync,
        CancellationToken cancellationToken);
}
