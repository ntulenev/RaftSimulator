using FluentAssertions;

using Moq;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Tests.TestSupport;
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
                new RaftVoteResponse(request.Term, new FromId(peer.Id), true));

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.RequestVotesAsync(new Term(2), new CandidateId(1), CancellationToken.None);

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
        var results = await broadcaster.SendHeartbeatsAsync(new Term(3), new LeaderId(1), CancellationToken.None);

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
        var results = await broadcaster.RequestVotesAsync(new Term(2), new CandidateId(1), CancellationToken.None);

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
        var results = await broadcaster.RequestVotesAsync(new Term(2), new CandidateId(1), CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result =>
            result.Response == null && result.Error!.GetType() == typeof(HttpRequestException));
    }

    [Fact(DisplayName = "SendHeartbeats captures peer timeouts")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsCapturesPeerTimeouts()
    {
        // Arrange
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        peerClient
            .Setup(client => client.AppendEntriesAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftAppendEntriesRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));

        var broadcaster = new RaftPeerBroadcaster(
            settings,
            peerClient.Object,
            new FixedDelayProvider());

        // Act
        var results = await broadcaster.SendHeartbeatsAsync(new Term(2), new LeaderId(1), CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(result =>
            result.Response == null && result.Error!.GetType() == typeof(TaskCanceledException));
    }

    [Fact(DisplayName = "RequestVotes propagates requested cancellation")]
    [Trait("Category", "Unit")]
    public async Task RequestVotesPropagatesRequestedCancellation()
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
        Func<Task> act = () => broadcaster.RequestVotesAsync(
            new Term(2),
            new CandidateId(1),
            source.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "RequestVotes rejects null domain arguments")]
    [Trait("Category", "Unit")]
    public async Task RequestVotesRejectsNullDomainArguments()
    {
        // Arrange
        var broadcaster = new RaftPeerBroadcaster(
            CreateSettings(),
            Mock.Of<IRaftPeerClient>(),
            new FixedDelayProvider());

        // Act
        Func<Task> nullTermAct = () => broadcaster.RequestVotesAsync(null!, new CandidateId(1), CancellationToken.None);
        Func<Task> nullCandidateAct = () => broadcaster.RequestVotesAsync(new Term(1), null!, CancellationToken.None);

        // Assert
        await nullTermAct.Should().ThrowAsync<ArgumentNullException>();
        await nullCandidateAct.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "SendHeartbeats rejects null domain arguments")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsRejectsNullDomainArguments()
    {
        // Arrange
        var broadcaster = new RaftPeerBroadcaster(
            CreateSettings(),
            Mock.Of<IRaftPeerClient>(),
            new FixedDelayProvider());

        // Act
        Func<Task> nullTermAct = () => broadcaster.SendHeartbeatsAsync(null!, new LeaderId(1), CancellationToken.None);
        Func<Task> nullLeaderAct = () => broadcaster.SendHeartbeatsAsync(new Term(1), null!, CancellationToken.None);

        // Assert
        await nullTermAct.Should().ThrowAsync<ArgumentNullException>();
        await nullLeaderAct.Should().ThrowAsync<ArgumentNullException>();
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
