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
    /// <param name="candidateId">Local candidate node identifier.</param>
    /// <param name="handleVoteResponseAsync">Vote response handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartElectionAsync(
        Term term,
        CandidateId candidateId,
        Func<RaftVoteResponse, CancellationToken, Task> handleVoteResponseAsync,
        CancellationToken cancellationToken);
}
