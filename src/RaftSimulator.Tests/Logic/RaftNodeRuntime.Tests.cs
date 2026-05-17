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
        var scenario = new RuntimeScenario();

        // Act
        var status = scenario.Node.GetStatus();

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
        var scenario = new RuntimeScenario();

        // Act
        var response = await scenario.Node.OnRequestVoteAsync(
            new RaftVoteRequest(2, 2),
            CancellationToken.None);

        // Assert
        response.Should().Be(new RaftVoteResponse(2, 1, true));
        scenario.EventLog.Events.Should().Contain(item => item.Event is RequestVoteGrantedEvent);
        scenario.Node.GetStatus().Term.Should().Be(new Term(2));
    }

    [Fact(DisplayName = "OnAppendEntries returns state machine append response and status snapshot")]
    [Trait("Category", "Unit")]
    public async Task OnAppendEntriesReturnsStateMachineAppendResponseAndStatusSnapshot()
    {
        // Arrange
        var scenario = new RuntimeScenario();

        // Act
        var response = await scenario.Node.OnAppendEntriesAsync(
            new RaftAppendEntriesRequest(3, 2),
            CancellationToken.None);

        // Assert
        response.Should().Be(new RaftAppendEntriesResponse(3, 1, true));
        scenario.EventLog.Events.Should().Contain(item => item.Event is HeartbeatReceivedEvent);
        scenario.Log.Statuses.Should().ContainSingle().Which.LeaderId.Should().Be(new LeaderId(2));
    }

    [Fact(DisplayName = "RunAsync starts election when scheduler timeout elapses")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncStartsElectionWhenSchedulerTimeoutElapses()
    {
        // Arrange
        var scenario = new RuntimeScenario()
            .AdvanceBeforeTimeout(TimeSpan.FromSeconds(5));

        // Act
        await scenario.Node.RunAsync(CancellationToken.None);

        // Assert
        scenario.ElectionRunner.StartElectionCalls.Should().Be(1);
        scenario.ElectionRunner.LastVoteTerm.Should().Be(1);
        scenario.ElectionRunner.LastCandidateId.Should().Be(1);
        scenario.EventLog.Events.Should().ContainSingle(item => item.Event is ElectionTimeoutEvent);
        scenario.Log.Messages.Should().Contain("Started as follower.");
        scenario.Log.Messages.Should().Contain("Stopped.");
    }

    [Fact(DisplayName = "RunAsync sends heartbeats after winning an election")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncSendsHeartbeatsAfterWinningAnElection()
    {
        // Arrange
        var scenario = new RuntimeScenario()
            .AdvanceBeforeTimeout(TimeSpan.FromSeconds(5))
            .GrantVoteFromFirstPeer()
            .AckHeartbeatFromFirstPeer();

        // Act
        await scenario.Node.RunAsync(CancellationToken.None);

        // Assert
        scenario.HeartbeatRunner.SendHeartbeatCalls.Should().Be(1);
        scenario.HeartbeatRunner.LastHeartbeatTerm.Should().Be(1);
        scenario.HeartbeatRunner.LastLeaderId.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync reports out of quorum from heartbeat runner callback")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncReportsOutOfQuorumFromHeartbeatRunnerCallback()
    {
        // Arrange
        var scenario = new RuntimeScenario()
            .AdvanceBeforeTimeout(TimeSpan.FromSeconds(5))
            .GrantVoteFromFirstPeer()
            .AdvanceBeforeQuorumReport(TimeSpan.FromSeconds(2));

        // Act
        await scenario.Node.RunAsync(CancellationToken.None);

        // Assert
        scenario.EventLog.Events.Should().ContainSingle(item => item.Event is OutOfQuorumEvent);
    }

    [Fact(DisplayName = "RunAsync handles higher term heartbeat response callback")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncHandlesHigherTermHeartbeatResponseCallback()
    {
        // Arrange
        var scenario = new RuntimeScenario()
            .AdvanceBeforeTimeout(TimeSpan.FromSeconds(5))
            .GrantVoteFromFirstPeer()
            .ReceiveHigherTermHeartbeatResponse();

        // Act
        await scenario.Node.RunAsync(CancellationToken.None);

        // Assert
        scenario.EventLog.Events.Should().Contain(item => item.Event is HigherTermDiscoveredEvent);
        scenario.Node.GetStatus().Role.Should().Be(RaftRole.Follower);
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

    private sealed class RuntimeScenario
    {
        public RuntimeScenario()
        {
            Scheduler = new TimeoutThenCancelScheduler(() => BeforeTimeout());
            Node = new RaftNode(
                Settings,
                Log,
                EventLog,
                Clock,
                new FixedDelayProvider(),
                Scheduler,
                ElectionRunner,
                HeartbeatRunner);
        }

        public RaftSettings Settings { get; } = CreateSettings();

        public TestClock Clock { get; } = new();

        public TimeoutThenCancelScheduler Scheduler { get; }

        public SpyElectionRunner ElectionRunner { get; } = new();

        public SpyHeartbeatRunner HeartbeatRunner { get; } = new();

        public TestRaftLog Log { get; } = new();

        public TestRaftEventLog EventLog { get; } = new();

        public RaftNode Node { get; }

        private Action BeforeTimeout { get; set; } = () => { };

        public RuntimeScenario AdvanceBeforeTimeout(TimeSpan value)
        {
            BeforeTimeout = () => Clock.Advance(value);
            return this;
        }

        public RuntimeScenario GrantVoteFromFirstPeer()
        {
            var peerId = Settings.Peers[0].Id;
            ElectionRunner.VoteResults = [new RaftVoteResponse(1, peerId, true)];
            return this;
        }

        public RuntimeScenario AckHeartbeatFromFirstPeer()
        {
            var peerId = Settings.Peers[0].Id;
            HeartbeatRunner.Responses = [new RaftAppendEntriesResponse(1, peerId, true)];
            HeartbeatRunner.AckPeerIds = [peerId];
            return this;
        }

        public RuntimeScenario AdvanceBeforeQuorumReport(TimeSpan value)
        {
            HeartbeatRunner.BeforeReportQuorum = () => Clock.Advance(value);
            return this;
        }

        public RuntimeScenario ReceiveHigherTermHeartbeatResponse()
        {
            HeartbeatRunner.Responses =
            [
                new RaftAppendEntriesResponse(2, Settings.Peers[0].Id, true)
            ];
            return this;
        }
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
        public IReadOnlyList<RaftVoteResponse> VoteResults { get; set; } = [];

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
        public IReadOnlyList<RaftAppendEntriesResponse> Responses { get; set; } = [];

        public IReadOnlyList<int> AckPeerIds { get; set; } = [];

        public Action? BeforeReportQuorum { get; set; }

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
