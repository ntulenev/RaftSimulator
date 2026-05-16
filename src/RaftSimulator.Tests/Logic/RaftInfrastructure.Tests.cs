using FluentAssertions;

using RaftSimulator.Logic;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftInfrastructureTests
{
    [Fact(DisplayName = "System clock returns current UTC time")]
    [Trait("Category", "Unit")]
    public void SystemClockReturnsCurrentUtcTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var clock = new SystemRaftClock();

        // Act
        var now = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        // Assert
        now.Should().BeOnOrAfter(before);
        now.Should().BeOnOrBefore(after);
    }

    [Fact(DisplayName = "Crypto random returns value in unit interval")]
    [Trait("Category", "Unit")]
    public void CryptoRandomReturnsValueInUnitInterval()
    {
        // Arrange
        var random = new CryptoRaftRandom();

        // Act
        var value = random.NextDouble();

        // Assert
        value.Should().BeGreaterThanOrEqualTo(0);
        value.Should().BeLessThan(1);
    }
}
