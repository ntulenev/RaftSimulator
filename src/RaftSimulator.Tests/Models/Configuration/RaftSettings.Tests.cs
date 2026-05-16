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

    [Fact(DisplayName = "FromOptions includes self when peers omit local node")]
    [Trait("Category", "Unit")]
    public void FromOptionsIncludesSelfWhenPeersOmitLocalNode()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Peers = "2=http://localhost:5002;3=http://localhost:5003";

        // Act
        var settings = RaftSettings.FromOptions(options);

        // Assert
        settings.NodeCount.Should().Be(3);
        settings.Majority.Should().Be(2);
        settings.Peers.Select(peer => peer.Id).Should().Equal(2, 3);
    }

    [Fact(DisplayName = "FromOptions rejects invalid data annotations")]
    [Trait("Category", "Unit")]
    public void FromOptionsWhenDataAnnotationsAreInvalidThrows()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HeartbeatSeconds = 0;

        // Act
        var act = () => RaftSettings.FromOptions(options);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory(DisplayName = "FromOptions rejects invalid node identity")]
    [Trait("Category", "Unit")]
    [InlineData(0, 5001, "The field NodeId must be between 1 and 2147483647.")]
    [InlineData(1, 0, "The field Port must be between 1 and 65535.")]
    public void FromOptionsWhenNodeIdentityIsInvalidThrows(
        int nodeId,
        int port,
        string expectedMessage)
    {
        // Arrange
        var options = CreateValidOptions();
        options.NodeId = nodeId;
        options.Port = port;

        // Act
        var act = () => RaftSettings.FromOptions(options);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(expectedMessage);
    }

    [Fact(DisplayName = "FromOptions rejects clusters smaller than three nodes")]
    [Trait("Category", "Unit")]
    public void FromOptionsWhenClusterHasFewerThanThreeNodesThrows()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Peers = "1=http://localhost:5001;2=http://localhost:5002";

        // Act
        var act = () => RaftSettings.FromOptions(options);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("RAFT requires at least 3 nodes.");
    }

    [Fact(DisplayName = "FromOptions rejects invalid election window")]
    [Trait("Category", "Unit")]
    public void FromOptionsWhenMaxElectionLessThanMinThrows()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MinElectionSeconds = 5;
        options.MaxElectionSeconds = 4;

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
        var options = CreateValidOptions();
        options.MinNetworkDelaySeconds = 2;
        options.MaxNetworkDelaySeconds = 1;

        // Act
        var act = () => RaftSettings.FromOptions(options);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Raft:MaxNetworkDelaySeconds must be >= Raft:MinNetworkDelaySeconds.");
    }

    private static RaftOptions CreateValidOptions() =>
        new()
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 4,
            MaxElectionSeconds = 7,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };
}
