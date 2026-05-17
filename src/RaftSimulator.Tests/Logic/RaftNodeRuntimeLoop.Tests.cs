using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftNodeRuntimeLoopTests
{
    [Fact(DisplayName = "RunAsync initializes and handles timeout")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncInitializesAndHandlesTimeout()
    {
        // Arrange
        var scheduler = new TimeoutThenCancelScheduler();
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
            CancellationToken.None);

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
        var scheduler = new TimeoutThenCancelScheduler();
        var runtime = new RaftNodeRuntime(scheduler, new TestRaftLog());

        // Act
        runtime.Signal();

        // Assert
        scheduler.SignalCalls.Should().Be(1);
    }

    private sealed class TimeoutThenCancelScheduler : IRaftScheduler
    {
        private int _waitCalls;

        public int SignalCalls { get; private set; }

        public void Signal() =>
            SignalCalls++;

        public Task<RaftScheduleWaitResult> WaitAsync(
            Func<TimeSpan> getNextDelay,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(getNextDelay);
            cancellationToken.ThrowIfCancellationRequested();

            _waitCalls++;
            if (_waitCalls == 1)
            {
                _ = getNextDelay();
                return Task.FromResult(RaftScheduleWaitResult.Timeout);
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class TestRaftLog : IRaftLog
    {
        public List<string> Messages { get; } = [];

        public void WriteNode(int nodeId, string message) =>
            Messages.Add(message);

        public void WriteSystem(string message) =>
            Messages.Add(message);

        public void WriteNodeStatus(RaftStatus status)
        {
        }
    }
}
