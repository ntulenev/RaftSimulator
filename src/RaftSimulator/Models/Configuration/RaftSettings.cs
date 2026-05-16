using System.ComponentModel.DataAnnotations;

namespace RaftSimulator.Models.Configuration;

/// <summary>
/// Immutable raft settings derived from configuration.
/// </summary>
internal sealed class RaftSettings
{
    private RaftSettings(
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
        ArgumentNullException.ThrowIfNull(options);

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);
        if (!Validator.TryValidateObject(
            options,
            validationContext,
            validationResults,
            validateAllProperties: true))
        {
            var message = string.Join(
                " ",
                validationResults.Select(static result => result.ErrorMessage));
            throw new InvalidOperationException(message);
        }

        if (options.NodeId < 1)
        {
            throw new InvalidOperationException("Raft:NodeId must be >= 1.");
        }

        if (options.Port < 1)
        {
            throw new InvalidOperationException("Raft:Port must be specified.");
        }

        if (options.MaxElectionSeconds < options.MinElectionSeconds)
        {
            throw new InvalidOperationException(
                "Raft:MaxElectionSeconds must be >= Raft:MinElectionSeconds.");
        }

        if (options.MaxNetworkDelaySeconds < options.MinNetworkDelaySeconds)
        {
            throw new InvalidOperationException(
                "Raft:MaxNetworkDelaySeconds must be >= Raft:MinNetworkDelaySeconds.");
        }

        var peers = PeerInfo.ParseList(options.Peers);
        var peerIds = new HashSet<int>(peers.Select(static peer => peer.Id));
        var nodeCount = peerIds.Contains(options.NodeId)
            ? peerIds.Count
            : peerIds.Count + 1;

        if (nodeCount < 3)
        {
            throw new InvalidOperationException("RAFT requires at least 3 nodes.");
        }

        peers = [.. peers.Where(peer => peer.Id != options.NodeId)];

        var heartbeat = TimeSpan.FromSeconds(options.HeartbeatSeconds);
        var minElection = TimeSpan.FromSeconds(options.MinElectionSeconds);
        var maxElection = TimeSpan.FromSeconds(options.MaxElectionSeconds);
        var minDelay = TimeSpan.FromSeconds(options.MinNetworkDelaySeconds);
        var maxDelay = TimeSpan.FromSeconds(options.MaxNetworkDelaySeconds);

        return new RaftSettings(
            options.NodeId,
            options.Port,
            peers,
            nodeCount,
            heartbeat,
            minElection,
            maxElection,
            minDelay,
            maxDelay);
    }
}
