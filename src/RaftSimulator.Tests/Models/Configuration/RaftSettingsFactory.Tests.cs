using FluentAssertions;

using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.Models.Configuration;

public sealed class RaftSettingsFactoryTests
{
    [Fact(DisplayName = "FromOptions builds validated settings")]
    [Trait("Category", "Unit")]
    public void FromOptionsBuildsValidatedSettings()
    {
        // Arrange
        var options = new RaftOptions
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 4,
            MaxElectionSeconds = 7,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 2
        };

        // Act
        var settings = RaftSettingsFactory.FromOptions(options);

        // Assert
        settings.NodeId.Should().Be(1);
        settings.Port.Should().Be(5001);
        settings.NodeCount.Should().Be(3);
        settings.Peers.Select(peer => peer.Id).Should().Equal(2, 3);
    }
}
