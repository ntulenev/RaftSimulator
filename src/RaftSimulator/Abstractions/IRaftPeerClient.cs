using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Raft peer client used for inter-node communication.
/// </summary>
internal interface IRaftPeerClient
{
    /// <summary>
    /// Requests a vote from a peer.
    /// </summary>
    /// <param name="peer">Target peer.</param>
    /// <param name="request">Vote request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vote response or null if the peer is unavailable.</returns>
    Task<RaftVoteResponse?> RequestVoteAsync(
        PeerInfo peer,
        RaftVoteRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends heartbeat or append-entries request to a peer.
    /// </summary>
    /// <param name="peer">Target peer.</param>
    /// <param name="request">Append-entries request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Append-entries response or null if the peer is unavailable.</returns>
    Task<RaftAppendEntriesResponse?> AppendEntriesAsync(
        PeerInfo peer,
        RaftAppendEntriesRequest request,
        CancellationToken cancellationToken);
}
