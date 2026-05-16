using FluentAssertions;

using Moq;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;

namespace RaftSimulator.Tests.Transport;

public sealed class RaftPeerBroadcasterTests
{
    [Fact(DisplayName = "RequestVotes returns one result per peer")]
    [Trait("Category", "Unit")]
    public async Task RequestVotesReturnsOneResultPerPeer()
    {
        // Arrange
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        peerClient
            .Setup(client => client.RequestVoteAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftVoteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PeerInfo peer, RaftVoteRequest request, CancellationToken _) =>
                new RaftVoteResponse(request.Term, peer.Id, true));

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.RequestVotesAsync(2, 1, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result => result.Response != null);
        results.Select(result => result.Response!.FromId.Value).Should().BeEquivalentTo([2, 3]);
    }

    [Fact(DisplayName = "SendHeartbeats marks null response as unavailable")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsMarksNullResponseAsUnavailable()
    {
        // Arrange
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        peerClient
            .Setup(client => client.AppendEntriesAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftAppendEntriesRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RaftAppendEntriesResponse?)null);

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.SendHeartbeatsAsync(3, 1, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result => result.Response == null && result.Error == null);
    }

    [Fact(DisplayName = "RequestVotes captures unexpected peer client errors")]
    [Trait("Category", "Unit")]
    public async Task RequestVotesCapturesUnexpectedPeerClientErrors()
    {
        // Arrange
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        peerClient
            .Setup(client => client.RequestVoteAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftVoteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.RequestVotesAsync(2, 1, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result =>
            result.Response == null && result.Error!.GetType() == typeof(InvalidOperationException));
    }

    [Fact(DisplayName = "RequestVotes captures transport errors")]
    [Trait("Category", "Unit")]
    public async Task RequestVotesCapturesTransportErrors()
    {
        // Arrange
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        peerClient
            .Setup(client => client.RequestVoteAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftVoteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.RequestVotesAsync(2, 1, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result =>
            result.Response == null && result.Error!.GetType() == typeof(HttpRequestException));
    }

    [Fact(DisplayName = "RequestVotes maps requested cancellation to unavailable peers")]
    [Trait("Category", "Unit")]
    public async Task RequestVotesMapsRequestedCancellationToUnavailablePeers()
    {
        // Arrange
        var settings = CreateSettings();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        peerClient
            .Setup(client => client.RequestVoteAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftVoteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(source.Token));

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.RequestVotesAsync(2, 1, source.Token);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result => result.Response == null && result.Error == null);
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

    private sealed class FixedDelayProvider : IRaftDelayProvider
    {
        public TimeSpan GetDelay(TimeSpan min, TimeSpan max) => min;
    }
}
