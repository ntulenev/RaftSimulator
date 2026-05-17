namespace RaftSimulator.Abstractions;

/// <summary>
/// Runs the raft node scheduling loop.
/// </summary>
internal interface IRaftNodeRuntime
{
    /// <summary>
    /// Signals that the schedule should be recalculated.
    /// </summary>
    void Signal();

    /// <summary>
    /// Runs the scheduling loop.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <param name="initialize">Initialization callback.</param>
    /// <param name="getNextDelay">Next delay callback.</param>
    /// <param name="handleTimeoutAsync">Timeout handler callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunAsync(
        int nodeId,
        Action initialize,
        Func<TimeSpan> getNextDelay,
        Func<CancellationToken, Task> handleTimeoutAsync,
        CancellationToken cancellationToken);
}
