using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Presentation;

/// <summary>
/// Formats typed raft events and writes them to the configured raft log.
/// </summary>
internal sealed class RaftEventLog : IRaftEventLog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftEventLog"/> class.
    /// </summary>
    /// <param name="log">Raft log.</param>
    public RaftEventLog(IRaftLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        _log = log;
    }

    /// <inheritdoc />
    public void WriteNodeEvent(int nodeId, RaftEvent raftEvent)
    {
        ArgumentNullException.ThrowIfNull(raftEvent);

        _log.WriteNode(nodeId, RaftEventFormatter.Format(raftEvent));
    }

    private readonly IRaftLog _log;
}
