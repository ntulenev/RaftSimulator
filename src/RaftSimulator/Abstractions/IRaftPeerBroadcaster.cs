using RaftSimulator.Models.Domain;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Broadcasts raft RPC calls to configured peers.
/// </summary>
internal interface IRaftPeerBroadcaster
{
    /// <summary>
    /// Requests votes from all configured peers.
    /// </summary>
    /// <param name="term">Election term.</param>
    /// <param name="candidateId">Candidate node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Peer RPC results.</returns>
    Task<IReadOnlyList<PeerRpcResult<RaftVoteResponse>>> RequestVotesAsync(
        Term term,
        CandidateId candidateId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends heartbeat append-entries RPC calls to all configured peers.
    /// </summary>
    /// <param name="term">Leader term.</param>
    /// <param name="leaderId">Leader node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Peer RPC results.</returns>
    Task<IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>>> SendHeartbeatsAsync(
        Term term,
        LeaderId leaderId,
        CancellationToken cancellationToken);
}
