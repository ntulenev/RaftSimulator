using FluentAssertions;

using RaftSimulator.Logic;
using RaftSimulator.Models.Domain.Events;
using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftQuorumEvaluatorTests
{
    [Fact(DisplayName = "BuildOutOfQuorumEvent returns null when majority is reachable")]
    [Trait("Category", "Unit")]
    public void BuildOutOfQuorumEventWhenMajorityIsReachableReturnsNull()
    {
        // Arrange
        var now = TestNow;
        var peers = CreatePeers();
        var acknowledgements = new Dictionary<int, DateTimeOffset>
        {
            [2] = now
        };

        // Act
        var quorumEvent = RaftQuorumEvaluator.BuildOutOfQuorumEvent(
            peers,
            acknowledgements,
            majority: 2,
            nodeCount: 3,
            now,
            TimeSpan.FromSeconds(1));

        // Assert
        quorumEvent.Should().BeNull();
    }

    [Fact(DisplayName = "BuildOutOfQuorumEvent ignores stale acknowledgements")]
    [Trait("Category", "Unit")]
    public void BuildOutOfQuorumEventIgnoresStaleAcknowledgements()
    {
        // Arrange
        var now = TestNow;
        var peers = CreatePeers();
        var acknowledgements = new Dictionary<int, DateTimeOffset>
        {
            [2] = now - TimeSpan.FromSeconds(2)
        };

        // Act
        var quorumEvent = RaftQuorumEvaluator.BuildOutOfQuorumEvent(
            peers,
            acknowledgements,
            majority: 2,
            nodeCount: 3,
            now,
            TimeSpan.FromSeconds(1));

        // Assert
        quorumEvent.Should().Be(new OutOfQuorumEvent(1, 3, 2));
    }

    private static IReadOnlyList<PeerInfo> CreatePeers() =>
        [
            new PeerInfo(2, new Uri("http://localhost:5002")),
            new PeerInfo(3, new Uri("http://localhost:5003"))
        ];

    private static readonly DateTimeOffset TestNow = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
}
