using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Logic;

/// <summary>
/// Evaluates whether a leader can still reach cluster quorum.
/// </summary>
internal static class RaftQuorumEvaluator
{
    /// <summary>
    /// Builds an out-of-quorum event when reachable nodes are below majority.
    /// </summary>
    /// <param name="peers">Configured peers excluding self.</param>
    /// <param name="lastHeartbeatAckAt">Last heartbeat acknowledgement by peer id.</param>
    /// <param name="majority">Majority threshold.</param>
    /// <param name="nodeCount">Total node count.</param>
    /// <param name="now">Current time.</param>
    /// <param name="window">Freshness window.</param>
    /// <returns>Out-of-quorum event, or null when quorum is reachable.</returns>
    public static OutOfQuorumEvent? BuildOutOfQuorumEvent(
        IReadOnlyList<PeerInfo> peers,
        IReadOnlyDictionary<int, DateTimeOffset> lastHeartbeatAckAt,
        int majority,
        int nodeCount,
        DateTimeOffset now,
        TimeSpan window)
    {
        ArgumentNullException.ThrowIfNull(peers);
        ArgumentNullException.ThrowIfNull(lastHeartbeatAckAt);

        var cutoff = now - window;
        var reachablePeers = 0;

        foreach (var peer in peers)
        {
            if (lastHeartbeatAckAt.TryGetValue(peer.Id, out var ackAt) && ackAt >= cutoff)
            {
                reachablePeers++;
            }
        }

        var reachable = reachablePeers + 1;
        return reachable >= majority
            ? null
            : new OutOfQuorumEvent(reachable, nodeCount, majority);
    }
}
