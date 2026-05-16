namespace RaftSimulator.Abstractions;

/// <summary>
/// Coordinates raft runtime wakeups.
/// </summary>
internal interface IRaftScheduler
{
    /// <summary>
    /// Signals that the raft schedule should be recalculated.
    /// </summary>
    void Signal();

    /// <summary>
    /// Waits until the next raft timeout or schedule-change signal.
    /// </summary>
    /// <param name="getNextDelay">Delegate that returns the current delay until next timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Wait result.</returns>
    Task<RaftScheduleWaitResult> WaitAsync(
        Func<TimeSpan> getNextDelay,
        CancellationToken cancellationToken);
}
