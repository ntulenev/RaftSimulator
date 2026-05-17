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
    /// <param name="reportQuorum">Quorum reporting callback.</param>
    /// <param name="handleAppendEntriesResponse">Append-entries response handler.</param>
    /// <param name="registerHeartbeatAck">Heartbeat acknowledgement callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendHeartbeatsAsync(
        int term,
        int nodeId,
        Action reportQuorum,
        Action<RaftAppendEntriesResponse> handleAppendEntriesResponse,
        Action<int> registerHeartbeatAck,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportQuorum);
        ArgumentNullException.ThrowIfNull(handleAppendEntriesResponse);
        ArgumentNullException.ThrowIfNull(registerHeartbeatAck);

        reportQuorum();

        var results = await _peerBroadcaster
            .SendHeartbeatsAsync(term, nodeId, cancellationToken)
            .ConfigureAwait(false);

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

            handleAppendEntriesResponse(result.Response);
            registerHeartbeatAck(result.Peer.Id);
        }
    }

    private readonly IRaftPeerBroadcaster _peerBroadcaster;
    private readonly IRaftLog _log;
}
