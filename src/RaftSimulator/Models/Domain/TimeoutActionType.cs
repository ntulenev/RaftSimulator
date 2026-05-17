namespace RaftSimulator.Models.Domain;

/// <summary>
/// Timeout action selected by the raft state machine.
/// </summary>
internal enum TimeoutActionType
{
    /// <summary>
    /// Start an election.
    /// </summary>
    Election,

    /// <summary>
    /// Send leader heartbeats.
    /// </summary>
    Heartbeats
}
