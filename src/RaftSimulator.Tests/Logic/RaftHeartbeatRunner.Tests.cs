using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftHeartbeatRunnerTests
{
    [Fact(DisplayName = "SendHeartbeats broadcasts to peers")]
    [Trait("Category", "Unit")]
    public async Task SendHeartbeatsBroadcastsToPeers()
    {
        // Arrange
        var broadcaster = new TestBroadcaster();
        var runner = new RaftHeartbeatRunner(broadcaster, new TestRaftLog());

        // Act
        _ = await runner.SendHeartbeatsAsync(
            4,
            1,
            CancellationToken.None);

        // Assert
        broadcaster.SendHeartbeatCalls.Should().Be(1);
        broadcaster.LastHeartbeatTerm.Should().Be(4);
        broadcaster.LastLeaderId.Should().Be(1);
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

        // Act
        var result = await runner.SendHeartbeatsAsync(
            4,
            1,
            CancellationToken.None);

        // Assert
        broadcaster.SendHeartbeatCalls.Should().Be(1);
        broadcaster.LastHeartbeatTerm.Should().Be(4);
        broadcaster.LastLeaderId.Should().Be(1);
        result.Responses.Should().ContainSingle().Which.Should().Be(response);
        result.AcknowledgedPeerIds.Should().ContainSingle().Which.Should().Be(2);
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

        // Act
        var result = await runner.SendHeartbeatsAsync(
            4,
            1,
            CancellationToken.None);

        // Assert
        result.AcknowledgedPeerIds.Should().BeEmpty();
        result.Responses.Should().BeEmpty();
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
        _ = await runner.SendHeartbeatsAsync(
            4,
            1,
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
        _ = await runner.SendHeartbeatsAsync(
            4,
            1,
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
