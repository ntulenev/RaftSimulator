using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftHeartbeatRunnerTests
{
    [Fact(DisplayName = "SendHeartbeats reports quorum before broadcasting")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsReportsQuorumBeforeBroadcasting()
    {
        // Arrange
        var order = new List<string>();
        var broadcaster = new TestBroadcaster { BeforeSend = () => order.Add("broadcast") };
        var runner = new RaftHeartbeatRunner(broadcaster, new TestRaftLog());

        // Act
        await runner.SendHeartbeatsAsync(
            4,
            1,
            () => order.Add("quorum"),
            _ => order.Add("response"),
            _ => order.Add("ack"),
            CancellationToken.None);

        // Assert
        order.Should().Equal("quorum", "broadcast");
    }

    [Fact(DisplayName = "SendHeartbeats handles successful append entries responses")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsHandlesSuccessfulAppendEntriesResponses()
    {
        // Arrange
        var peer = CreatePeer(2);
        var response = new RaftAppendEntriesResponse(4, 2, true);
        var broadcaster = new TestBroadcaster
        {
            HeartbeatResults =
            [
                PeerRpcResult<RaftAppendEntriesResponse>.Success(peer, response)
            ]
        };
        var log = new TestRaftLog();
        var runner = new RaftHeartbeatRunner(broadcaster, log);
        var handled = new List<RaftAppendEntriesResponse>();
        var acknowledgements = new List<int>();

        // Act
        await runner.SendHeartbeatsAsync(
            4,
            1,
            () => { },
            handled.Add,
            acknowledgements.Add,
            CancellationToken.None);

        // Assert
        broadcaster.SendHeartbeatCalls.Should().Be(1);
        broadcaster.LastHeartbeatTerm.Should().Be(4);
        broadcaster.LastLeaderId.Should().Be(1);
        handled.Should().ContainSingle().Which.Should().Be(response);
        acknowledgements.Should().ContainSingle().Which.Should().Be(2);
        log.Messages.Should().Contain("AppendEntries -> Node 02 (term 4).");
    }

    [Fact(DisplayName = "SendHeartbeats logs unavailable responses without acknowledging")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsLogsUnavailableResponsesWithoutAcknowledging()
    {
        // Arrange
        var peer = CreatePeer(2);
        var broadcaster = new TestBroadcaster
        {
            HeartbeatResults = [PeerRpcResult<RaftAppendEntriesResponse>.Unavailable(peer)]
        };
        var log = new TestRaftLog();
        var runner = new RaftHeartbeatRunner(broadcaster, log);
        var acknowledgements = new List<int>();

        // Act
        await runner.SendHeartbeatsAsync(
            4,
            1,
            () => { },
            _ => { },
            acknowledgements.Add,
            CancellationToken.None);

        // Assert
        acknowledgements.Should().BeEmpty();
        log.Messages.Should().Contain("AppendEntries unavailable from Node 02.");
    }

    [Fact(DisplayName = "SendHeartbeats logs peer failures")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsLogsPeerFailures()
    {
        // Arrange
        var peer = CreatePeer(2);
        var broadcaster = new TestBroadcaster
        {
            HeartbeatResults =
            [
                PeerRpcResult<RaftAppendEntriesResponse>.Failed(
                    peer,
                    new InvalidOperationException("boom"))
            ]
        };
        var log = new TestRaftLog();
        var runner = new RaftHeartbeatRunner(broadcaster, log);

        // Act
        await runner.SendHeartbeatsAsync(
            4,
            1,
            () => { },
            _ => { },
            _ => { },
            CancellationToken.None);

        // Assert
        log.Messages.Should().Contain(
            "AppendEntries (term 4) -> Node 02 failed: InvalidOperationException: boom");
    }

    [Fact(DisplayName = "SendHeartbeats logs transport failures as unreachable")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsLogsTransportFailuresAsUnreachable()
    {
        // Arrange
        var peer = CreatePeer(2);
        var broadcaster = new TestBroadcaster
        {
            HeartbeatResults =
            [
                PeerRpcResult<RaftAppendEntriesResponse>.Failed(
                    peer,
                    new TaskCanceledException("timeout"))
            ]
        };
        var log = new TestRaftLog();
        var runner = new RaftHeartbeatRunner(broadcaster, log);

        // Act
        await runner.SendHeartbeatsAsync(
            4,
            1,
            () => { },
            _ => { },
            _ => { },
            CancellationToken.None);

        // Assert
        log.Messages.Should().Contain("Unable to reach Node 02.");
    }

    private static PeerInfo CreatePeer(int id) =>
        new(id, new Uri($"http://localhost:500{id}"));

    private sealed class TestBroadcaster : IRaftPeerBroadcaster
    {
        public IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>> HeartbeatResults { get; init; } =
            [];

        public Action? BeforeSend { get; init; }

        public int SendHeartbeatCalls { get; private set; }

        public int LastHeartbeatTerm { get; private set; }

        public int LastLeaderId { get; private set; }

        public Task<IReadOnlyList<PeerRpcResult<RaftVoteResponse>>> RequestVotesAsync(
            int term,
            int candidateId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>>> SendHeartbeatsAsync(
            int term,
            int leaderId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BeforeSend?.Invoke();
            SendHeartbeatCalls++;
            LastHeartbeatTerm = term;
            LastLeaderId = leaderId;
            return Task.FromResult(HeartbeatResults);
        }
    }

    private sealed class TestRaftLog : IRaftLog
    {
        public List<string> Messages { get; } = [];

        public void WriteNode(int nodeId, string message) =>
            Messages.Add(message);

        public void WriteSystem(string message) =>
            Messages.Add(message);

        public void WriteNodeStatus(RaftStatus status)
        {
        }
    }
}
