using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftNodeRuntimeTests
{
    [Fact(DisplayName = "GetStatus returns initialized follower status")]
    [Trait("Category", "Unit")]
    public void GetStatusReturnsInitializedFollowerStatus()
    {
        // Arrange
        var node = CreateNode();

        // Act
        var status = node.GetStatus();

        // Assert
        status.NodeId.Should().Be(new NodeId(1));
        status.Term.Should().Be(new Term(0));
        status.Role.Should().Be(RaftRole.Follower);
        status.LeaderId.Should().BeNull();
    }

    [Fact(DisplayName = "OnRequestVote returns state machine vote response")]
    [Trait("Category", "Unit")]
    public async Task OnRequestVoteReturnsStateMachineVoteResponse()
    {
        // Arrange
        var eventLog = new TestRaftEventLog();
        var node = CreateNode(eventLog: eventLog);

        // Act
        var response = await node.OnRequestVoteAsync(
            new RaftVoteRequest(2, 2),
            CancellationToken.None);

        // Assert
        response.Should().Be(new RaftVoteResponse(2, 1, true));
        eventLog.Events.Should().Contain(item => item.Event is RequestVoteGrantedEvent);
        node.GetStatus().Term.Should().Be(new Term(2));
    }

    [Fact(DisplayName = "OnAppendEntries returns state machine append response and status snapshot")]
    [Trait("Category", "Unit")]
    public async Task OnAppendEntriesReturnsStateMachineAppendResponseAndStatusSnapshot()
    {
        // Arrange
        var log = new TestRaftLog();
        var eventLog = new TestRaftEventLog();
        var node = CreateNode(log, eventLog);

        // Act
        var response = await node.OnAppendEntriesAsync(
            new RaftAppendEntriesRequest(3, 2),
            CancellationToken.None);

        // Assert
        response.Should().Be(new RaftAppendEntriesResponse(3, 1, true));
        eventLog.Events.Should().Contain(item => item.Event is HeartbeatReceivedEvent);
        log.Statuses.Should().ContainSingle().Which.LeaderId.Should().Be(new LeaderId(2));
    }

    [Fact(DisplayName = "RunAsync starts election when scheduler timeout elapses")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncStartsElectionWhenSchedulerTimeoutElapses()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var scheduler = new TimeoutThenCancelScheduler(() => clock.Advance(TimeSpan.FromSeconds(5)));
        var electionRunner = new SpyElectionRunner();
        var log = new TestRaftLog();
        var eventLog = new TestRaftEventLog();
        var node = new RaftNode(
            settings,
            log,
            eventLog,
            clock,
            new FixedDelayProvider(),
            scheduler,
            electionRunner,
            new SpyHeartbeatRunner());

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        electionRunner.StartElectionCalls.Should().Be(1);
        electionRunner.LastVoteTerm.Should().Be(1);
        electionRunner.LastCandidateId.Should().Be(1);
        eventLog.Events.Should().ContainSingle(item => item.Event is ElectionTimeoutEvent);
        log.Messages.Should().Contain("Started as follower.");
        log.Messages.Should().Contain("Stopped.");
    }

    [Fact(DisplayName = "RunAsync sends heartbeats after winning an election")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncSendsHeartbeatsAfterWinningAnElection()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var scheduler = new TimeoutThenCancelScheduler(() => clock.Advance(TimeSpan.FromSeconds(5)));
        var electionRunner = new SpyElectionRunner
        {
            VoteResults =
            [
                new RaftVoteResponse(1, settings.Peers[0].Id, true)
            ]
        };
        var heartbeatRunner = new SpyHeartbeatRunner
        {
            Responses =
            [
                new RaftAppendEntriesResponse(1, settings.Peers[0].Id, true)
            ],
            AckPeerIds = [settings.Peers[0].Id]
        };
        var node = new RaftNode(
            settings,
            new TestRaftLog(),
            new TestRaftEventLog(),
            clock,
            new FixedDelayProvider(),
            scheduler,
            electionRunner,
            heartbeatRunner);

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        heartbeatRunner.SendHeartbeatCalls.Should().Be(1);
        heartbeatRunner.LastHeartbeatTerm.Should().Be(1);
        heartbeatRunner.LastLeaderId.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync reports out of quorum from heartbeat runner callback")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncReportsOutOfQuorumFromHeartbeatRunnerCallback()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var scheduler = new TimeoutThenCancelScheduler(() => clock.Advance(TimeSpan.FromSeconds(5)));
        var eventLog = new TestRaftEventLog();
        var electionRunner = new SpyElectionRunner
        {
            VoteResults =
            [
                new RaftVoteResponse(1, settings.Peers[0].Id, true)
            ]
        };
        var heartbeatRunner = new SpyHeartbeatRunner
        {
            BeforeReportQuorum = () => clock.Advance(TimeSpan.FromSeconds(2))
        };
        var node = new RaftNode(
            settings,
            new TestRaftLog(),
            eventLog,
            clock,
            new FixedDelayProvider(),
            scheduler,
            electionRunner,
            heartbeatRunner);

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        eventLog.Events.Should().ContainSingle(item => item.Event is OutOfQuorumEvent);
    }

    [Fact(DisplayName = "RunAsync handles higher term heartbeat response callback")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncHandlesHigherTermHeartbeatResponseCallback()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var scheduler = new TimeoutThenCancelScheduler(() => clock.Advance(TimeSpan.FromSeconds(5)));
        var eventLog = new TestRaftEventLog();
        var electionRunner = new SpyElectionRunner
        {
            VoteResults =
            [
                new RaftVoteResponse(1, settings.Peers[0].Id, true)
            ]
        };
        var heartbeatRunner = new SpyHeartbeatRunner
        {
            Responses =
            [
                new RaftAppendEntriesResponse(2, settings.Peers[0].Id, true)
            ]
        };
        var node = new RaftNode(
            settings,
            new TestRaftLog(),
            eventLog,
            clock,
            new FixedDelayProvider(),
            scheduler,
            electionRunner,
            heartbeatRunner);

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        eventLog.Events.Should().Contain(item => item.Event is HigherTermDiscoveredEvent);
        node.GetStatus().Role.Should().Be(RaftRole.Follower);
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
            MaxElectionSeconds = 4,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };

        return RaftSettings.FromOptions(options);
    }

    private static RaftNode CreateNode(
        TestRaftLog? log = null,
        TestRaftEventLog? eventLog = null)
    {
        var settings = CreateSettings();
        return new RaftNode(
            settings,
            log ?? new TestRaftLog(),
            eventLog ?? new TestRaftEventLog(),
            new TestClock(),
            new FixedDelayProvider(),
            new TimeoutThenCancelScheduler(() => { }),
            new SpyElectionRunner(),
            new SpyHeartbeatRunner());
    }

    private sealed class TimeoutThenCancelScheduler(Action beforeTimeout) : IRaftScheduler
    {
        private int _waitCalls;

        public int SignalCalls { get; private set; }

        public void Signal() => SignalCalls++;

        public Task<RaftScheduleWaitResult> WaitAsync(
            Func<TimeSpan> getNextDelay,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(getNextDelay);
            cancellationToken.ThrowIfCancellationRequested();

            _waitCalls++;
            if (_waitCalls == 1)
            {
                _ = getNextDelay();
                beforeTimeout();
                return Task.FromResult(RaftScheduleWaitResult.Timeout);
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class TestClock : IRaftClock
    {
        public DateTimeOffset UtcNow { get; private set; } =
            new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan value) =>
            UtcNow += value;
    }

    private sealed class FixedDelayProvider : IRaftDelayProvider
    {
        public TimeSpan GetDelay(TimeSpan min, TimeSpan max) => min;
    }

    private sealed class SpyElectionRunner : IRaftElectionRunner
    {
        public IReadOnlyList<RaftVoteResponse> VoteResults { get; init; } = [];

        public int StartElectionCalls { get; private set; }

        public int LastVoteTerm { get; private set; }

        public int LastCandidateId { get; private set; }

        public async Task StartElectionAsync(
            int term,
            int candidateId,
            Func<RaftVoteResponse, CancellationToken, Task> handleVoteResponseAsync,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(handleVoteResponseAsync);
            cancellationToken.ThrowIfCancellationRequested();

            StartElectionCalls++;
            LastVoteTerm = term;
            LastCandidateId = candidateId;

            foreach (var response in VoteResults)
            {
                await handleVoteResponseAsync(response, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class SpyHeartbeatRunner : IRaftHeartbeatRunner
    {
        public IReadOnlyList<RaftAppendEntriesResponse> Responses { get; init; } = [];

        public IReadOnlyList<int> AckPeerIds { get; init; } = [];

        public Action? BeforeReportQuorum { get; init; }

        public int SendHeartbeatCalls { get; private set; }

        public int LastHeartbeatTerm { get; private set; }

        public int LastLeaderId { get; private set; }

        public Task SendHeartbeatsAsync(
            int term,
            int leaderId,
            Action reportQuorum,
            Action<RaftAppendEntriesResponse> handleAppendEntriesResponse,
            Action<int> registerHeartbeatAck,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reportQuorum);
            ArgumentNullException.ThrowIfNull(handleAppendEntriesResponse);
            ArgumentNullException.ThrowIfNull(registerHeartbeatAck);
            cancellationToken.ThrowIfCancellationRequested();

            SendHeartbeatCalls++;
            LastHeartbeatTerm = term;
            LastLeaderId = leaderId;
            BeforeReportQuorum?.Invoke();
            reportQuorum();
            foreach (var response in Responses)
            {
                handleAppendEntriesResponse(response);
            }

            foreach (var peerId in AckPeerIds)
            {
                registerHeartbeatAck(peerId);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestRaftLog : IRaftLog
    {
        public List<string> Messages { get; } = [];

        public List<RaftStatus> Statuses { get; } = [];

        public void WriteNode(int nodeId, string message) =>
            Messages.Add(message);

        public void WriteSystem(string message) =>
            Messages.Add(message);

        public void WriteNodeStatus(RaftStatus status) =>
            Statuses.Add(status);
    }

    private sealed class TestRaftEventLog : IRaftEventLog
    {
        public List<(int NodeId, RaftEvent Event)> Events { get; } = [];

        public void WriteNodeEvent(int nodeId, RaftEvent raftEvent) =>
            Events.Add((nodeId, raftEvent));
    }
}
