using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// Hosted service that runs raft node loop.
/// </summary>
internal sealed class RaftHostedService : BackgroundService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftHostedService"/> class.
    /// </summary>
    /// <param name="node">Raft node.</param>
    public RaftHostedService(IRaftNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        _node = node;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _node.RunAsync(stoppingToken);

    private readonly IRaftNode _node;
}
