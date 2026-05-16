using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftDelayProviderTests
{
    [Fact(DisplayName = "GetDelay returns minimum when range has no window")]
    [Trait("Category", "Unit")]
    public void GetDelayReturnsMinimumWhenRangeHasNoWindow()
    {
        // Arrange
        var provider = new RaftDelayProvider(new FixedRandom(0.5));
        var delay = TimeSpan.FromSeconds(4);

        // Act
        var result = provider.GetDelay(delay, delay);

        // Assert
        result.Should().Be(delay);
    }

    [Fact(DisplayName = "GetDelay returns offset inside range")]
    [Trait("Category", "Unit")]
    public void GetDelayReturnsOffsetInsideRange()
    {
        // Arrange
        var provider = new RaftDelayProvider(new FixedRandom(0.25));

        // Act
        var result = provider.GetDelay(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8));

        // Assert
        result.Should().Be(TimeSpan.FromSeconds(5));
    }

    private sealed class FixedRandom(double value) : IRaftRandom
    {
        public double NextDouble() => value;
    }
}
