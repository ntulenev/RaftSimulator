using FluentAssertions;

using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftStateMachineTests
{
    [Fact(DisplayName = "RequestVote denies lower term requests")]
    [Trait("Category", "Unit")]
    public void RequestVoteWhenTermIsLowerDenies()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        _ = machine.HandleRequestVote(
            new RaftVoteRequest(2, 2),
            now,
            TimeSpan.FromSeconds(4));

        // Act
        var decision = machine.HandleRequestVote(
            new RaftVoteRequest(1, 3),
            now,
            TimeSpan.FromSeconds(4));

        // Assert
        decision.Response.Granted.Should().BeFalse();
        decision.Response.Term.Should().Be(new Term(2));
        machine.GetStatus().Term.Should().Be(new Term(2));
    }

    [Fact(DisplayName = "AppendEntries updates leader and term")]
    [Trait("Category", "Unit")]
    public void AppendEntriesUpdatesLeaderAndTerm()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var decision = machine.HandleAppendEntries(
            new RaftAppendEntriesRequest(3, 5),
            now,
            TimeSpan.FromSeconds(4));
        var status = machine.GetStatus();

        // Assert
        decision.Response.Success.Should().BeTrue();
        status.Term.Should().Be(new Term(3));
        status.LeaderId.Should().Be(new LeaderId(5));
        status.Role.Should().Be(RaftRole.Follower);
    }

    [Fact(DisplayName = "Vote majority transitions candidate to leader")]
    [Trait("Category", "Unit")]
    public void VoteMajorityTransitionsCandidateToLeader()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var timeout = machine.PrepareTimeoutAction(
            now,
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(1));

        // Act
        var decision = machine.HandleVoteResponse(
            new RaftVoteResponse(timeout.Term, 2, true),
            now,
            TimeSpan.FromSeconds(4));
        var status = machine.GetStatus();

        // Assert
        decision.BecameLeader.Should().BeTrue();
        decision.StatusSnapshot.Should().NotBeNull();
        status.Role.Should().Be(RaftRole.Leader);
        status.LeaderId.Should().Be(new LeaderId(1));
    }

    [Fact(DisplayName = "AppendEntries response with higher term steps down")]
    [Trait("Category", "Unit")]
    public void AppendEntriesResponseWithHigherTermStepsDown()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var timeout = machine.PrepareTimeoutAction(
            now,
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(1));
        _ = machine.HandleVoteResponse(
            new RaftVoteResponse(timeout.Term, 2, true),
            now,
            TimeSpan.FromSeconds(4));

        // Act
        var decision = machine.HandleAppendEntriesResponse(
            new RaftAppendEntriesResponse(4, 2, true),
            now,
            TimeSpan.FromSeconds(4));
        var status = machine.GetStatus();

        // Assert
        decision.Events.Should().HaveCount(2);
        status.Term.Should().Be(new Term(4));
        status.Role.Should().Be(RaftRole.Follower);
        status.LeaderId.Should().BeNull();
    }

    private static RaftStateMachine CreateStateMachine() =>
        new(CreateSettings());

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

    private static readonly DateTimeOffset TestNow = new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);
}
