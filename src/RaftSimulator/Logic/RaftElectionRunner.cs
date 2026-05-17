using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;

namespace RaftSimulator.Logic;

/// <summary>
/// Runs request-vote RPC orchestration for a raft node.
/// </summary>
internal sealed class RaftElectionRunner : IRaftElectionRunner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftElectionRunner"/> class.
    /// </summary>
    /// <param name="peerBroadcaster">Peer broadcaster.</param>
    /// <param name="log">Log sink.</param>
    public RaftElectionRunner(IRaftPeerBroadcaster peerBroadcaster, IRaftLog log)
    {
        ArgumentNullException.ThrowIfNull(peerBroadcaster);
        ArgumentNullException.ThrowIfNull(log);

        _peerBroadcaster = peerBroadcaster;
        _log = log;
    }

    /// <summary>
    /// Starts an election and handles received vote responses.
    /// </summary>
    /// <param name="term">Election term.</param>
    /// <param name="nodeId">Local node identifier.</param>
    /// <param name="handleVoteResponseAsync">Vote response handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartElectionAsync(
        int term,
        int nodeId,
        Func<RaftVoteResponse, CancellationToken, Task> handleVoteResponseAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handleVoteResponseAsync);

        var results = await _peerBroadcaster
            .RequestVotesAsync(term, nodeId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var result in results)
        {
            _log.WriteNode(nodeId, $"RequestVote -> Node {result.Peer.Id:00} (term {term}).");

            if (result.Error is not null)
            {
                _log.WriteNode(
                    nodeId,
                    PeerRpcLogFormatter.FormatFailure(
                        "RequestVote",
                        result.Peer.Id,
                        term,
                        result.Error));
                continue;
            }

            if (result.Response is null)
            {
                _log.WriteNode(nodeId, $"VoteResponse unavailable from Node {result.Peer.Id:00}.");
                continue;
            }

            await handleVoteResponseAsync(result.Response, cancellationToken).ConfigureAwait(false);
        }
    }

    private readonly IRaftPeerBroadcaster _peerBroadcaster;
    private readonly IRaftLog _log;
}
