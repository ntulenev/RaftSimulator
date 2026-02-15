using System.ComponentModel.DataAnnotations;

namespace RaftSimulator.Models.Configuration;

/// <summary>
/// Raft options loaded from configuration.
/// </summary>
internal sealed class RaftOptions
{
    /// <summary>
    /// Gets or sets node identifier.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int NodeId { get; set; }

    /// <summary>
    /// Gets or sets local HTTP port.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets peer list in "id=http://host:port" format.
    /// </summary>
    public string? Peers { get; set; }

    /// <summary>
    /// Gets or sets heartbeat interval in seconds.
    /// </summary>
    [Range(1, 60)]
    public int HeartbeatSeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets minimum election timeout in seconds.
    /// </summary>
    [Range(1, 120)]
    public int MinElectionSeconds { get; set; } = 4;

    /// <summary>
    /// Gets or sets maximum election timeout in seconds.
    /// </summary>
    [Range(1, 120)]
    public int MaxElectionSeconds { get; set; } = 7;

    /// <summary>
    /// Gets or sets minimum simulated network delay in seconds.
    /// </summary>
    [Range(0, 30)]
    public int MinNetworkDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets maximum simulated network delay in seconds.
    /// </summary>
    [Range(0, 30)]
    public int MaxNetworkDelaySeconds { get; set; } = 2;
}
