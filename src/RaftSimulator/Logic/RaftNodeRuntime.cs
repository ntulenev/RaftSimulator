using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// Raft node scheduling loop.
/// </summary>
internal sealed class RaftNodeRuntime : IRaftNodeRuntime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftNodeRuntime"/> class.
    /// </summary>
    /// <param name="scheduler">Runtime scheduler.</param>
    /// <param name="log">Log sink.</param>
    public RaftNodeRuntime(IRaftScheduler scheduler, IRaftLog log)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(log);

        _scheduler = scheduler;
        _log = log;
    }

    /// <inheritdoc />
    public void Signal() =>
        _scheduler.Signal();

    /// <inheritdoc />
    public async Task RunAsync(
        int nodeId,
        Action initialize,
        Func<TimeSpan> getNextDelay,
        Func<CancellationToken, Task> handleTimeoutAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initialize);
        ArgumentNullException.ThrowIfNull(getNextDelay);
        ArgumentNullException.ThrowIfNull(handleTimeoutAsync);

        initialize();
        _log.WriteNode(nodeId, "Started as follower.");

        try
        {
            while (true)
            {
                var waitResult = await _scheduler
                    .WaitAsync(getNextDelay, cancellationToken)
                    .ConfigureAwait(false);

                if (waitResult == RaftScheduleWaitResult.Signaled)
                {
                    continue;
                }

                await handleTimeoutAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            _log.WriteNode(nodeId, "Stopped.");
        }
    }

    private readonly IRaftScheduler _scheduler;
    private readonly IRaftLog _log;
}
