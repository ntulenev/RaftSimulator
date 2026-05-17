using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;

namespace RaftSimulator.Logic;

/// <summary>
/// Runs append-entries heartbeat RPC orchestration for a raft node.
/// </summary>
internal sealed class RaftHeartbeatRunner : IRaftHeartbeatRunner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftHeartbeatRunner"/> class.
    /// </summary>
    /// <param name="peerBroadcaster">Peer broadcaster.</param>
    /// <param name="log">Log sink.</param>
    public RaftHeartbeatRunner(IRaftPeerBroadcaster peerBroadcaster, IRaftLog log)
    {
        ArgumentNullException.ThrowIfNull(peerBroadcaster);
        ArgumentNullException.ThrowIfNull(log);

        _peerBroadcaster = peerBroadcaster;
        _log = log;
    }

    /// <summary>
    /// Sends heartbeat RPC calls and handles received append-entries responses.
    /// </summary>
    /// <param name="term">Leader term.</param>
    /// <param name="nodeId">Local node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Heartbeat run result.</returns>
    public async Task<HeartbeatRunResult> SendHeartbeatsAsync(
        int term,
        int nodeId,
        CancellationToken cancellationToken)
    {
        var results = await _peerBroadcaster
            .SendHeartbeatsAsync(term, nodeId, cancellationToken)
            .ConfigureAwait(false);
        var responses = new List<RaftAppendEntriesResponse>(results.Count);
        var acknowledgedPeerIds = new List<int>(results.Count);

        foreach (var result in results)
        {
            _log.WriteNode(nodeId, $"AppendEntries -> Node {result.Peer.Id:00} (term {term}).");

            if (result.Error is not null)
            {
                _log.WriteNode(
                    nodeId,
                    PeerRpcLogFormatter.FormatFailure(
                        "AppendEntries",
                        result.Peer.Id,
                        term,
                        result.Error));
                continue;
            }

            if (result.Response is null)
            {
                _log.WriteNode(nodeId, $"AppendEntries unavailable from Node {result.Peer.Id:00}.");
                continue;
            }

            responses.Add(result.Response);
            acknowledgedPeerIds.Add(result.Peer.Id);
        }

        return new HeartbeatRunResult(responses, acknowledgedPeerIds);
    }

    private readonly IRaftPeerBroadcaster _peerBroadcaster;
    private readonly IRaftLog _log;
}
