using System.ComponentModel.DataAnnotations;

namespace RaftSimulator.Models.Configuration;

/// <summary>
/// Creates validated raft settings from configuration options.
/// </summary>
internal static class RaftSettingsFactory
{
    /// <summary>
    /// Builds settings from options.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <returns>Settings.</returns>
    public static RaftSettings FromOptions(RaftOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        var peers = PeerInfo.ParseList(options.Peers);
        var nodeCount = GetNodeCount(options.NodeId, peers);
        if (nodeCount < 3)
        {
            throw new InvalidOperationException("RAFT requires at least 3 nodes.");
        }

        return new RaftSettings(
            options.NodeId,
            options.Port,
            [.. peers.Where(peer => peer.Id != options.NodeId)],
            nodeCount,
            TimeSpan.FromSeconds(options.HeartbeatSeconds),
            TimeSpan.FromSeconds(options.MinElectionSeconds),
            TimeSpan.FromSeconds(options.MaxElectionSeconds),
            TimeSpan.FromSeconds(options.MinNetworkDelaySeconds),
            TimeSpan.FromSeconds(options.MaxNetworkDelaySeconds));
    }

    private static void ValidateOptions(RaftOptions options)
    {
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
    }

    private static int GetNodeCount(int nodeId, IReadOnlyList<PeerInfo> peers)
    {
        var peerIds = new HashSet<int>(peers.Select(static peer => peer.Id));
        return peerIds.Contains(nodeId)
            ? peerIds.Count
            : peerIds.Count + 1;
    }
}
