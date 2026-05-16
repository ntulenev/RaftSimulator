using System.Threading.Channels;

using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// Channel-backed raft runtime scheduler.
/// </summary>
internal sealed class RaftScheduler : IRaftScheduler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftScheduler"/> class.
    /// </summary>
    public RaftScheduler()
    {
        _scheduleSignal = Channel.CreateUnbounded<bool>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    /// <inheritdoc />
    public void Signal() =>
        _ = _scheduleSignal.Writer.TryWrite(true);

    /// <inheritdoc />
    public async Task<RaftScheduleWaitResult> WaitAsync(
        Func<TimeSpan> getNextDelay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(getNextDelay);

        var delay = getNextDelay();
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        var waitToken = waitCancellation.Token;
        var delayTask = Task.Delay(delay, waitToken);
        var signalTask = _scheduleSignal.Reader.ReadAsync(waitToken).AsTask();

        var completed = await Task
            .WhenAny(delayTask, signalTask)
            .ConfigureAwait(false);

        if (completed == signalTask)
        {
            _ = await signalTask.ConfigureAwait(false);
            DrainScheduleSignals();
            await waitCancellation.CancelAsync().ConfigureAwait(false);
            return RaftScheduleWaitResult.Signaled;
        }

        await delayTask.ConfigureAwait(false);
        await waitCancellation.CancelAsync().ConfigureAwait(false);
        return RaftScheduleWaitResult.Timeout;
    }

    private void DrainScheduleSignals()
    {
        while (_scheduleSignal.Reader.TryRead(out _))
        {
        }
    }

    private readonly Channel<bool> _scheduleSignal;
}
