using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftSchedulerTests
{
    [Fact(DisplayName = "WaitAsync returns timeout when delay elapses")]
    [Trait("Category", "Unit")]
    public async Task WaitAsyncReturnsTimeoutWhenDelayElapses()
    {
        // Arrange
        var scheduler = new RaftScheduler();

        // Act
        var result = await scheduler.WaitAsync(() => TimeSpan.Zero, CancellationToken.None);

        // Assert
        result.Should().Be(RaftScheduleWaitResult.Timeout);
    }

    [Fact(DisplayName = "WaitAsync returns signaled when signal is queued")]
    [Trait("Category", "Unit")]
    public async Task WaitAsyncReturnsSignaledWhenSignalIsQueued()
    {
        // Arrange
        var scheduler = new RaftScheduler();
        scheduler.Signal();

        // Act
        var result = await scheduler.WaitAsync(() => TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        result.Should().Be(RaftScheduleWaitResult.Signaled);
    }

    [Fact(DisplayName = "WaitAsync drains queued signals")]
    [Trait("Category", "Unit")]
    public async Task WaitAsyncDrainsQueuedSignals()
    {
        // Arrange
        var scheduler = new RaftScheduler();
        scheduler.Signal();
        scheduler.Signal();

        // Act
        var first = await scheduler.WaitAsync(() => TimeSpan.FromMinutes(1), CancellationToken.None);
        var second = await scheduler.WaitAsync(() => TimeSpan.Zero, CancellationToken.None);

        // Assert
        first.Should().Be(RaftScheduleWaitResult.Signaled);
        second.Should().Be(RaftScheduleWaitResult.Timeout);
    }

    [Fact(DisplayName = "WaitAsync observes cancellation")]
    [Trait("Category", "Unit")]
    public async Task WaitAsyncObservesCancellation()
    {
        // Arrange
        var scheduler = new RaftScheduler();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => scheduler.WaitAsync(() => TimeSpan.FromMinutes(1), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
