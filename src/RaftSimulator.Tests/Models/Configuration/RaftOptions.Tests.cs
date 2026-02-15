using FluentAssertions;

using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.Models.Configuration;

public sealed class RaftOptionsTests
{
    [Fact(DisplayName = "Default values match expected settings")]
    [Trait("Category", "Unit")]
    public void DefaultsMatchExpectedValues()
    {
        // Act
        var options = new RaftOptions();

        // Assert
        options.HeartbeatSeconds.Should().Be(1);
        options.MinElectionSeconds.Should().Be(4);
        options.MaxElectionSeconds.Should().Be(7);
        options.MinNetworkDelaySeconds.Should().Be(1);
        options.MaxNetworkDelaySeconds.Should().Be(2);
    }
}
