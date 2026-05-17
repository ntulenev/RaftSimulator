using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftNodeCoordinatorTests
{
    [Fact(DisplayName = "Constructor rejects null dependencies")]
    [Trait("Category", "Unit")]
    public void ConstructorRejectsNullDependencies()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var delayProvider = new FixedDelayProvider();
        var runtime = new TestRuntime();

        // Act
        Action[] acts =
        [
            () => _ = new RaftNodeCoordinator(null!, clock, delayProvider, runtime),
            () => _ = new RaftNodeCoordinator(settings, null!, delayProvider, runtime),
            () => _ = new RaftNodeCoordinator(settings, clock, null!, runtime),
            () => _ = new RaftNodeCoordinator(settings, clock, delayProvider, null!)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentNullException>();
        }
    }

    [Fact(DisplayName = "InitializeState initializes follower and signals scheduler")]
    [Trait("Category", "Unit")]
    public void InitializeStateInitializesFollowerAndSignalsScheduler()
    {
        // Arrange
        var runtime = new TestRuntime();
        var coordinator = CreateCoordinator(runtime: runtime);

        // Act
        coordinator.InitializeState();
        var status = coordinator.GetStatus();

        // Assert
        status.Role.Should().Be(RaftRole.Follower);
        status.Term.Should().Be(Term.Initial);
        coordinator.GetNextDelay().Should().Be(TimeSpan.FromSeconds(4));
        runtime.SignalCalls.Should().Be(1);
    }

    [Fact(DisplayName = "HandleRequestVote delegates transition and signals scheduler")]
    [Trait("Category", "Unit")]
    public void HandleRequestVoteDelegatesTransitionAndSignalsScheduler()
    {
        // Arrange
        var runtime = new TestRuntime();
        var coordinator = CreateInitializedCoordinator(runtime: runtime);

        // Act
        var decision = coordinator.HandleRequestVote(new RaftVoteRequest(2, 2));
        var status = coordinator.GetStatus();

        // Assert
        decision.Response.Should().Be(new RaftVoteResponse(2, 1, true));
        status.Term.Should().Be(new Term(2));
        status.Role.Should().Be(RaftRole.Follower);
        runtime.SignalCalls.Should().Be(2);
    }

    [Fact(DisplayName = "HandleAppendEntries publishes status snapshot")]
    [Trait("Category", "Unit")]
    public void HandleAppendEntriesPublishesStatusSnapshot()
    {
        // Arrange
        var coordinator = CreateInitializedCoordinator();

        // Act
        var decision = coordinator.HandleAppendEntries(new RaftAppendEntriesRequest(2, 2));

        // Assert
        decision.Response.Should().Be(new RaftAppendEntriesResponse(2, 1, true));
        decision.StatusSnapshot.Should().NotBeNull();
        coordinator.GetStatus().LeaderId.Should().Be(new LeaderId(2));
    }

    [Fact(DisplayName = "PrepareTimeoutAction starts election")]
    [Trait("Category", "Unit")]
    public void PrepareTimeoutActionStartsElection()
    {
        // Arrange
        var coordinator = CreateInitializedCoordinator();

        // Act
        var action = coordinator.PrepareTimeoutAction();

        // Assert
        action.Type.Should().Be(TimeoutActionType.Election);
        action.Term.Should().Be(new Term(1));
        coordinator.GetStatus().Role.Should().Be(RaftRole.Candidate);
    }

    [Fact(DisplayName = "Vote response can make node leader and heartbeat ack can restore quorum")]
    [Trait("Category", "Unit")]
    public void VoteResponseCanMakeNodeLeaderAndHeartbeatAckCanRestoreQuorum()
    {
        // Arrange
        var clock = new TestClock();
        var coordinator = CreateInitializedCoordinator(clock: clock);
        var timeout = coordinator.PrepareTimeoutAction();

        // Act
        var voteDecision = coordinator.HandleVoteResponse(new RaftVoteResponse(timeout.Term, 2, true));
        var status = coordinator.GetStatus();
        clock.Advance(TimeSpan.FromSeconds(3));
        var beforeAck = coordinator.BuildQuorumEvent(TimeSpan.FromSeconds(2));
        coordinator.RegisterHeartbeatAck(2);
        var afterAck = coordinator.BuildQuorumEvent(TimeSpan.FromSeconds(2));

        // Assert
        voteDecision.BecameLeader.Should().BeTrue();
        status.Role.Should().Be(RaftRole.Leader);
        beforeAck.Should().NotBeNull();
        afterAck.Should().BeNull();
    }

    [Fact(DisplayName = "HandleAppendEntriesResponse steps down on higher term")]
    [Trait("Category", "Unit")]
    public void HandleAppendEntriesResponseStepsDownOnHigherTerm()
    {
        // Arrange
        var coordinator = CreateInitializedCoordinator();
        var timeout = coordinator.PrepareTimeoutAction();
        _ = coordinator.HandleVoteResponse(new RaftVoteResponse(timeout.Term, 2, true));

        // Act
        var decision = coordinator.HandleAppendEntriesResponse(new RaftAppendEntriesResponse(2, 2, true));

        // Assert
        decision.Events.Should().HaveCount(2);
        coordinator.GetStatus().Role.Should().Be(RaftRole.Follower);
    }

    [Fact(DisplayName = "Public handlers reject null arguments")]
    [Trait("Category", "Unit")]
    public void PublicHandlersRejectNullArguments()
    {
        // Arrange
        var coordinator = CreateInitializedCoordinator();

        // Act
        Action[] acts =
        [
            () => _ = coordinator.HandleRequestVote(null!),
            () => _ = coordinator.HandleAppendEntries(null!),
            () => _ = coordinator.HandleVoteResponse(null!),
            () => _ = coordinator.HandleAppendEntriesResponse(null!),
            () => coordinator.RegisterHeartbeatAck(null!)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentNullException>();
        }
    }

    private static RaftNodeCoordinator CreateInitializedCoordinator(
        TestClock? clock = null,
        TestRuntime? runtime = null)
    {
        var coordinator = CreateCoordinator(clock, runtime);
        coordinator.InitializeState();
        return coordinator;
    }

    private static RaftNodeCoordinator CreateCoordinator(
        TestClock? clock = null,
        TestRuntime? runtime = null) =>
        new(
            CreateSettings(),
            clock ?? new TestClock(),
            new FixedDelayProvider(),
            runtime ?? new TestRuntime());

    private static RaftSettings CreateSettings()
    {
        var options = new RaftOptions
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 4,
            MaxElectionSeconds = 4,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };

        return RaftSettings.FromOptions(options);
    }

    private sealed class TestClock : IRaftClock
    {
        public DateTimeOffset UtcNow { get; private set; } =
            new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan value) =>
            UtcNow += value;
    }

    private sealed class FixedDelayProvider : IRaftDelayProvider
    {
        public TimeSpan GetDelay(TimeSpan min, TimeSpan max) => min;
    }

    private sealed class TestRuntime : IRaftNodeRuntime
    {
        public int SignalCalls { get; private set; }

        public void Signal() =>
            SignalCalls++;

        public Task RunAsync(
            int nodeId,
            Action initialize,
            Func<TimeSpan> getNextDelay,
            Func<CancellationToken, Task> handleTimeoutAsync,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
