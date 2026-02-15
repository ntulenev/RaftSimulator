using FluentAssertions;

using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.Models.Configuration;

public sealed class RaftSettingsTests
{
    [Fact(DisplayName = "FromOptions calculates majority and excludes self")]
    [Trait("Category", "Unit")]
    public void FromOptionsCalculatesMajorityAndExcludesSelf()
    {
        // Arrange
        var options = new RaftOptions
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003;4=http://localhost:5004;5=http://localhost:5005",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 4,
            MaxElectionSeconds = 7,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };

        // Act
        var settings = RaftSettings.FromOptions(options);

        // Assert
        settings.NodeCount.Should().Be(5);
        settings.Majority.Should().Be(3);
        settings.Peers.Should().HaveCount(4);
        settings.Peers.Select(peer => peer.Id).Should().NotContain(1);
    }

    [Fact(DisplayName = "FromOptions rejects invalid election window")]
    [Trait("Category", "Unit")]
    public void FromOptionsWhenMaxElectionLessThanMinThrows()
    {
        // Arrange
        var options = new RaftOptions
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 5,
            MaxElectionSeconds = 4,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };

        // Act
        var act = () => RaftSettings.FromOptions(options);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Raft:MaxElectionSeconds must be >= Raft:MinElectionSeconds.");
    }

    [Fact(DisplayName = "FromOptions rejects invalid network delay window")]
    [Trait("Category", "Unit")]
    public void FromOptionsWhenMaxNetworkDelayLessThanMinThrows()
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
            MinNetworkDelaySeconds = 2,
            MaxNetworkDelaySeconds = 1
        };

        // Act
        var act = () => RaftSettings.FromOptions(options);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Raft:MaxNetworkDelaySeconds must be >= Raft:MinNetworkDelaySeconds.");
    }
}
