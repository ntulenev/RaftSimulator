namespace RaftSimulator.Models.Configuration;

/// <summary>
/// Immutable raft settings derived from configuration.
/// </summary>
internal sealed class RaftSettings
{
    internal RaftSettings(
        int nodeId,
        int port,
        IReadOnlyList<PeerInfo> peers,
        int nodeCount,
        TimeSpan heartbeatInterval,
        TimeSpan minElectionTimeout,
        TimeSpan maxElectionTimeout,
        TimeSpan minNetworkDelay,
        TimeSpan maxNetworkDelay)
    {
        NodeId = nodeId;
        Port = port;
        Peers = peers;
        NodeCount = nodeCount;
        HeartbeatInterval = heartbeatInterval;
        MinElectionTimeout = minElectionTimeout;
        MaxElectionTimeout = maxElectionTimeout;
        MinNetworkDelay = minNetworkDelay;
        MaxNetworkDelay = maxNetworkDelay;
    }

    /// <summary>
    /// Gets node identifier.
    /// </summary>
    public int NodeId { get; }

    /// <summary>
    /// Gets local HTTP port.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets configured peers (excluding self).
    /// </summary>
    public IReadOnlyList<PeerInfo> Peers { get; }

    /// <summary>
    /// Gets total node count (including self).
    /// </summary>
    public int NodeCount { get; }

    /// <summary>
    /// Gets majority threshold.
    /// </summary>
    public int Majority => (NodeCount / 2) + 1;

    /// <summary>
    /// Gets heartbeat interval.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; }

    /// <summary>
    /// Gets minimum election timeout.
    /// </summary>
    public TimeSpan MinElectionTimeout { get; }

    /// <summary>
    /// Gets maximum election timeout.
    /// </summary>
    public TimeSpan MaxElectionTimeout { get; }

    /// <summary>
    /// Gets minimum simulated network delay.
    /// </summary>
    public TimeSpan MinNetworkDelay { get; }

    /// <summary>
    /// Gets maximum simulated network delay.
    /// </summary>
    public TimeSpan MaxNetworkDelay { get; }

    /// <summary>
    /// Builds settings from options.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <returns>Settings.</returns>
    public static RaftSettings FromOptions(RaftOptions options)
    {
        return RaftSettingsFactory.FromOptions(options);
    }
}
