namespace RaftSimulator.Abstractions;

/// <summary>
/// Result of waiting for the next raft schedule action.
/// </summary>
internal enum RaftScheduleWaitResult
{
    /// <summary>
    /// Timeout elapsed.
    /// </summary>
    Timeout,

    /// <summary>
    /// Schedule-change signal was received.
    /// </summary>
    Signaled
}
