using RaftSimulator.Models.Domain;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Raft node state machine abstraction.
/// </summary>
internal interface IRaftNode
{
    /// <summary>
    /// Gets node identifier.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Runs node state machine loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Handles request vote RPC.
    /// </summary>
    /// <param name="request">Vote request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vote response.</returns>
    Task<RaftVoteResponse> OnRequestVoteAsync(
        RaftVoteRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Handles append entries RPC.
    /// </summary>
    /// <param name="request">Append entries request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Append entries response.</returns>
    Task<RaftAppendEntriesResponse> OnAppendEntriesAsync(
        RaftAppendEntriesRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns current status snapshot.
    /// </summary>
    /// <returns>Status.</returns>
    RaftStatus GetStatus();
}
