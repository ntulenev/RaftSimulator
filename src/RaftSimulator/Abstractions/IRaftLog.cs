using RaftSimulator.Models.Domain;

namespace RaftSimulator.Abstractions;

/// <summary>
/// Raft logging abstraction.
/// </summary>
internal interface IRaftLog
{
    /// <summary>
    /// Writes a node-scoped log entry.
    /// </summary>
    /// <param name="nodeId">Node id.</param>
    /// <param name="message">Message.</param>
    void WriteNode(int nodeId, string message);

    /// <summary>
    /// Writes a system log entry.
    /// </summary>
    /// <param name="message">Message.</param>
    void WriteSystem(string message);

    /// <summary>
    /// Writes a node status snapshot.
    /// </summary>
    /// <param name="status">Raft status.</param>
    void WriteNodeStatus(RaftStatus status);
}
