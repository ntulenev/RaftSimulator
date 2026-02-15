using FluentAssertions;

using Moq;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftNodeTests
{
    [Fact(DisplayName = "OnRequestVote denies lower term requests")]
    [Trait("Category", "Unit")]
    public async Task OnRequestVoteWhenTermIsLowerDenies()
    {
        // Arrange
        var node = CreateNode();

        await node.OnRequestVoteAsync(new RaftVoteRequest(2, 2), CancellationToken.None);

        // Act
        var response = await node
            .OnRequestVoteAsync(new RaftVoteRequest(1, 3), CancellationToken.None);

        // Assert
        response.Granted.Should().BeFalse();
        response.Term.Should().Be(2);
    }

    [Fact(DisplayName = "OnAppendEntries updates leader and term")]
    [Trait("Category", "Unit")]
    public async Task OnAppendEntriesUpdatesLeaderAndTerm()
    {
        // Arrange
        var node = CreateNode();

        // Act
        var response = await node
            .OnAppendEntriesAsync(new RaftAppendEntriesRequest(3, 5), CancellationToken.None);

        var status = node.GetStatus();

        // Assert
        response.Success.Should().BeTrue();
        status.Term.Should().Be(3);
        status.LeaderId.Should().Be(5);
        status.Role.Should().Be(RaftRole.Follower);
    }

    private static RaftNode CreateNode()
    {
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Loose).Object;
        var log = new Mock<IRaftLog>(MockBehavior.Loose).Object;
        return new RaftNode(settings, peerClient, log);
    }

    private static RaftSettings CreateSettings()
    {
        var options = new RaftOptions
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

        return RaftSettings.FromOptions(options);
    }
}
