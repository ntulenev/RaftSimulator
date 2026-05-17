using FluentAssertions;

using RaftSimulator.Logic;
using RaftSimulator.Models.Domain.Events;
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

    [Fact(DisplayName = "Accepted heartbeat resets follower election deadline")]
    [Trait("Category", "Unit")]
    public void AcceptedHeartbeatResetsFollowerElectionDeadline()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var heartbeatAt = now + TimeSpan.FromSeconds(3);

        // Act
        var decision = machine.HandleAppendEntries(
            new RaftAppendEntriesRequest(1, 2),
            heartbeatAt,
            TimeSpan.FromSeconds(5));
        var delay = machine.GetNextDelay(heartbeatAt + TimeSpan.FromSeconds(4));

        // Assert
        decision.Response.Success.Should().BeTrue();
        delay.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "AppendEntries denies stale leader term")]
    [Trait("Category", "Unit")]
    public void AppendEntriesDeniesStaleLeaderTerm()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        _ = machine.HandleRequestVote(
            new RaftVoteRequest(3, 2),
            now,
            TimeSpan.FromSeconds(4));

        // Act
        var decision = machine.HandleAppendEntries(
            new RaftAppendEntriesRequest(2, 3),
            now,
            TimeSpan.FromSeconds(4));

        // Assert
        decision.Response.Success.Should().BeFalse();
        decision.Events.Should().ContainSingle().Which.Should().BeOfType<HeartbeatIgnoredEvent>();
    }

    [Fact(DisplayName = "AppendEntries from current leader emits single status snapshot per term")]
    [Trait("Category", "Unit")]
    public void AppendEntriesFromCurrentLeaderEmitsSingleStatusSnapshotPerTerm()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var first = machine.HandleAppendEntries(
            new RaftAppendEntriesRequest(2, 3),
            now,
            TimeSpan.FromSeconds(4));
        var second = machine.HandleAppendEntries(
            new RaftAppendEntriesRequest(2, 3),
            now,
            TimeSpan.FromSeconds(4));

        // Assert
        first.StatusSnapshot.Should().NotBeNull();
        first.Events.Should().HaveCount(2);
        second.StatusSnapshot.Should().BeNull();
        second.Events.Should().ContainSingle();
    }

    [Fact(DisplayName = "Leader reports out of quorum when heartbeat acknowledgements are stale")]
    [Trait("Category", "Unit")]
    public void LeaderReportsOutOfQuorumWhenHeartbeatAcknowledgementsAreStale()
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
        var quorumEvent = machine.BuildQuorumEvent(
            now + TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(2));

        // Assert
        quorumEvent.Should().BeEquivalentTo(new OutOfQuorumEvent(1, 3, 2));
    }

    [Fact(DisplayName = "Leader does not report quorum before freshness window elapses")]
    [Trait("Category", "Unit")]
    public void LeaderDoesNotReportQuorumBeforeFreshnessWindowElapses()
    {
        // Arrange
        var machine = CreateLeaderStateMachine();

        // Act
        var quorumEvent = machine.BuildQuorumEvent(
            TestNow + TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2));

        // Assert
        quorumEvent.Should().BeNull();
    }

    [Fact(DisplayName = "Leader does not report out of quorum when majority is reachable")]
    [Trait("Category", "Unit")]
    public void LeaderDoesNotReportOutOfQuorumWhenMajorityIsReachable()
    {
        // Arrange
        var machine = CreateLeaderStateMachine();
        machine.RegisterHeartbeatAck(2, TestNow + TimeSpan.FromSeconds(3));

        // Act
        var quorumEvent = machine.BuildQuorumEvent(
            TestNow + TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(2));

        // Assert
        quorumEvent.Should().BeNull();
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

    [Fact(DisplayName = "AppendEntries response with current term is ignored")]
    [Trait("Category", "Unit")]
    public void AppendEntriesResponseWithCurrentTermIsIgnored()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var decision = machine.HandleAppendEntriesResponse(
            new RaftAppendEntriesResponse(0, 2, true),
            now,
            TimeSpan.FromSeconds(4));

        // Assert
        decision.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Vote response from higher term steps candidate down")]
    [Trait("Category", "Unit")]
    public void VoteResponseFromHigherTermStepsCandidateDown()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        _ = machine.PrepareTimeoutAction(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var decision = machine.HandleVoteResponse(
            new RaftVoteResponse(3, 2, false),
            now,
            TimeSpan.FromSeconds(4));
        var status = machine.GetStatus();

        // Assert
        decision.Events.Should().HaveCount(2);
        status.Role.Should().Be(RaftRole.Follower);
        status.Term.Should().Be(new Term(3));
    }

    [Fact(DisplayName = "Vote response is ignored when node is not candidate")]
    [Trait("Category", "Unit")]
    public void VoteResponseIsIgnoredWhenNodeIsNotCandidate()
    {
        // Arrange
        var machine = CreateStateMachine();
        var now = TestNow;
        machine.Initialize(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var decision = machine.HandleVoteResponse(
            new RaftVoteResponse(0, 2, true),
            now,
            TimeSpan.FromSeconds(4));

        // Assert
        decision.Events.Should().BeEmpty();
        decision.BecameLeader.Should().BeFalse();
    }

    [Fact(DisplayName = "Vote denial emits denial event")]
    [Trait("Category", "Unit")]
    public void VoteDenialEmitsDenialEvent()
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
            new RaftVoteResponse(timeout.Term, 2, false),
            now,
            TimeSpan.FromSeconds(4));

        // Assert
        decision.Events.Should().ContainSingle().Which.Should().BeOfType<VoteResponseDeniedEvent>();
    }

    [Fact(DisplayName = "Leader timeout prepares heartbeat action")]
    [Trait("Category", "Unit")]
    public void LeaderTimeoutPreparesHeartbeatAction()
    {
        // Arrange
        var machine = CreateLeaderStateMachine();

        // Act
        var action = machine.PrepareTimeoutAction(
            TestNow,
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(1));

        // Assert
        action.Type.Should().Be(TimeoutActionType.Heartbeats);
        action.Events.Should().ContainSingle().Which.Should().BeOfType<LeaderHeartbeatEvent>();
    }

    [Fact(DisplayName = "AppendEntries decision exposes events")]
    [Trait("Category", "Unit")]
    public void AppendEntriesDecisionExposesEvents()
    {
        // Arrange
        var raftEvent = new HeartbeatReceivedEvent(2, 1);

        // Act
        var decision = new AppendEntriesDecision(
            new RaftAppendEntriesResponse(1, 1, true),
            [raftEvent],
            null);

        // Assert
        decision.Events.Should().ContainSingle().Which.Should().Be(raftEvent);
    }

    private static RaftStateMachine CreateStateMachine() =>
        new(CreateSettings());

    private static RaftStateMachine CreateLeaderStateMachine()
    {
        var machine = CreateStateMachine();
        machine.Initialize(TestNow, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var timeout = machine.PrepareTimeoutAction(
            TestNow,
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(1));
        _ = machine.HandleVoteResponse(
            new RaftVoteResponse(timeout.Term, 2, true),
            TestNow,
            TimeSpan.FromSeconds(4));
        return machine;
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

    private static readonly DateTimeOffset TestNow = new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);
}
