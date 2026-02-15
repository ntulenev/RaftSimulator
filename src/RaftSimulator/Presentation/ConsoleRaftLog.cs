using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Presentation;

/// <summary>
/// Console-based raft logger.
/// </summary>
internal sealed class ConsoleRaftLog : IRaftLog
{
    /// <inheritdoc />
    public void WriteNode(int nodeId, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var line =
            $"[{DateTimeOffset.Now:HH:mm:ss.fff}] [Node {nodeId:00}] {message}";
        WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteSystem(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var line = $"[{DateTimeOffset.Now:HH:mm:ss.fff}] [System] {message}";
        WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteNodeStatus(RaftStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var leaderText = status.LeaderId is null ? "unknown" : $"{status.LeaderId:00}";
        var line =
            $"[{DateTimeOffset.Now:HH:mm:ss.fff}] [Node {status.NodeId:00}] " +
            $"Status: id={status.NodeId:00}, role={status.Role}, leader={leaderText}.";
        WriteLine(line, ConsoleColor.Red);
    }

    private static void WriteLine(string line, ConsoleColor? color = null)
    {
        lock (_gate)
        {
            var previous = Console.ForegroundColor;
            if (color is not null)
            {
                Console.ForegroundColor = color.Value;
            }

            Console.WriteLine(line);

            if (color is not null)
            {
                Console.ForegroundColor = previous;
            }
        }
    }

    private static readonly Lock _gate = new();
}
