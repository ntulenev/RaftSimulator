using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;

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
    /// <param name="leaderId">Local leader node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Heartbeat run result.</returns>
    public async Task<HeartbeatRunResult> SendHeartbeatsAsync(
        Term term,
        LeaderId leaderId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(leaderId);

        var results = await _peerBroadcaster
            .SendHeartbeatsAsync(term, leaderId, cancellationToken)
            .ConfigureAwait(false);
        var responses = new List<RaftAppendEntriesResponse>(results.Count);
        var acknowledgedPeerIds = new List<int>(results.Count);

        foreach (var result in results)
        {
            _log.WriteNode(leaderId.Value, $"AppendEntries -> Node {result.Peer.Id:00} (term {term}).");

            if (result.Error is not null)
            {
                _log.WriteNode(
                    leaderId.Value,
                    PeerRpcLogFormatter.FormatFailure(
                        "AppendEntries",
                        result.Peer.Id,
                        term.Value,
                        result.Error));
                continue;
            }

            if (result.Response is null)
            {
                _log.WriteNode(leaderId.Value, $"AppendEntries unavailable from Node {result.Peer.Id:00}.");
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
