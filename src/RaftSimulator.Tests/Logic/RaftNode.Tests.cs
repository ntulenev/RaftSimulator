using System.Reflection;

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
        response.Term.Should().Be(new Term(2));
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
        status.Term.Should().Be(new Term(3));
        status.LeaderId.Should().Be(new LeaderId(5));
        status.Role.Should().Be(RaftRole.Follower);
    }

    [Fact(DisplayName = "Append entries response with higher term steps down")]
    [Trait("Category", "Unit")]
    public async Task AppendEntriesResponseWithHigherTermStepsDown()
    {
        // Arrange
        var settings = CreateSettings();
        var peer = settings.Peers[0];
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        var log = new Mock<IRaftLog>(MockBehavior.Loose);
        var node = new RaftNode(settings, peerClient.Object, log.Object);

        SetPrivateField(node, "_role", RaftRole.Leader);
        SetPrivateField(node, "_currentTerm", 3);
        SetPrivateField(node, "_leaderId", (int?)settings.NodeId);

        peerClient
            .Setup(client => client.AppendEntriesAsync(
                peer,
                It.IsAny<RaftAppendEntriesRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RaftAppendEntriesResponse(4, peer.Id, true));

        // Act
        await InvokePrivateAsync(
            node,
            "SendHeartbeatToPeerAsync",
            peer,
            3,
            CancellationToken.None);

        // Assert
        peerClient.Verify(client => client.AppendEntriesAsync(
            peer,
            It.IsAny<RaftAppendEntriesRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        var status = node.GetStatus();
        status.Term.Should().Be(new Term(4));
        status.Role.Should().Be(RaftRole.Follower);
        status.LeaderId.Should().BeNull();
    }

    [Fact(DisplayName = "StartElection logs unexpected peer errors")]
    [Trait("Category", "Unit")]
    public async Task StartElectionLogsUnexpectedPeerErrors()
    {
        // Arrange
        var settings = CreateSettings();
        var peerClient = new Mock<IRaftPeerClient>(MockBehavior.Strict);
        var log = new Mock<IRaftLog>(MockBehavior.Loose);
        var failureLogged = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        log.Setup(logger => logger.WriteNode(
                It.IsAny<int>(),
                It.Is<string>(message => message.Contains("failed"))))
            .Callback<int, string>((_, message) => failureLogged.TrySetResult(message));

        peerClient
            .Setup(client => client.RequestVoteAsync(
                It.IsAny<PeerInfo>(),
                It.IsAny<RaftVoteRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var node = new RaftNode(settings, peerClient.Object, log.Object);

        // Act
        await InvokePrivateAsync(node, "StartElectionAsync", 1, CancellationToken.None);

        // Assert
        var completed = await Task.WhenAny(failureLogged.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        completed.Should().BeSameAs(failureLogged.Task);
        var message = await failureLogged.Task;
        message.Should().Contain("InvalidOperationException");
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

    private static Task InvokePrivateAsync(object target, string methodName, params object?[] args)
    {
        var method = target
            .GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        var task = (Task?)method!.Invoke(target, args);
        task.Should().NotBeNull();
        return task!;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull();
        field!.SetValue(target, value);
    }
}
