using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Tests.TestSupport;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftNodeRuntimeLoopTests
{
    [Fact(DisplayName = "RunAsync initializes and handles timeout")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncInitializesAndHandlesTimeout()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var scheduler = new TimeoutThenCancelScheduler(cancellation);
        var log = new TestRaftLog();
        var runtime = new RaftNodeRuntime(scheduler, log);
        var initialized = false;
        var timeoutCalls = 0;

        // Act
        await runtime.RunAsync(
            1,
            () => initialized = true,
            () => TimeSpan.Zero,
            _ =>
            {
                timeoutCalls++;
                return Task.CompletedTask;
            },
            cancellation.Token);

        // Assert
        initialized.Should().BeTrue();
        timeoutCalls.Should().Be(1);
        log.Messages.Should().Contain("Started as follower.");
        log.Messages.Should().Contain("Stopped.");
    }

    [Fact(DisplayName = "Signal forwards schedule signal")]
    [Trait("Category", "Unit")]
    public void SignalForwardsScheduleSignal()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var scheduler = new TimeoutThenCancelScheduler(cancellation);
        var runtime = new RaftNodeRuntime(scheduler, new TestRaftLog());

        // Act
        runtime.Signal();

        // Assert
        scheduler.SignalCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync propagates unexpected cancellation")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncPropagatesUnexpectedCancellation()
    {
        // Arrange
        var runtime = new RaftNodeRuntime(new UnexpectedCancelScheduler(), new TestRaftLog());

        // Act
        var act = () => runtime.RunAsync(
            1,
            () => { },
            () => TimeSpan.Zero,
            _ => Task.CompletedTask,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class TimeoutThenCancelScheduler(CancellationTokenSource cancellation) : IRaftScheduler
    {
        private int _waitCalls;

        public int SignalCalls { get; private set; }

        public void Signal() =>
            SignalCalls++;

        public async Task<RaftScheduleWaitResult> WaitAsync(
            Func<TimeSpan> getNextDelay,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(getNextDelay);
            cancellationToken.ThrowIfCancellationRequested();

            _waitCalls++;
            if (_waitCalls == 1)
            {
                _ = getNextDelay();
                return RaftScheduleWaitResult.Timeout;
            }

            await cancellation.CancelAsync().ConfigureAwait(false);
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class UnexpectedCancelScheduler : IRaftScheduler
    {
        public void Signal()
        {
        }

        public Task<RaftScheduleWaitResult> WaitAsync(
            Func<TimeSpan> getNextDelay,
            CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }

}
