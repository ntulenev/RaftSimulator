using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Logs typed raft events.
/// </summary>
internal interface IRaftEventLog
{
    /// <summary>
    /// Writes a node-scoped raft event.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <param name="raftEvent">Raft event.</param>
    void WriteNodeEvent(int nodeId, RaftEvent raftEvent);
}
