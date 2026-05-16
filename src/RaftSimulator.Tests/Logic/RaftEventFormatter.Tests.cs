using FluentAssertions;

using RaftSimulator.Logic;
using RaftSimulator.Logic.Events;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftEventFormatterTests
{
    [Fact(DisplayName = "Format returns expected log messages")]
    [Trait("Category", "Unit")]
    public void FormatReturnsExpectedLogMessages()
    {
        // Arrange
        (RaftEvent Event, string Expected)[] cases =
        [
            (new RequestVoteDeniedEvent(2, 1), "Denied vote to Node 02 (term 1)."),
            (new RequestVoteGrantedEvent(2, 1), "Granted vote to Node 02 (term 1)."),
            (new BecameFollowerEvent(3, null), "Became follower for term 3 (leader unknown)."),
            (new BecameFollowerEvent(3, 2), "Became follower for term 3 (leader Node 02)."),
            (new HeartbeatIgnoredEvent(2, 1), "Ignored heartbeat from Node 02 (term 1)."),
            (new HeartbeatReceivedEvent(2, 3), "Heartbeat from Node 02 (term 3)."),
            (new LeaderHeartbeatEvent(), "Leader heartbeat."),
            (new ElectionTimeoutEvent(4), "Election timeout. Term 4, becoming candidate."),
            (new HigherTermDiscoveredEvent(5, 2), "Discovered higher term 5 from Node 02."),
            (new VoteResponseDeniedEvent(2, 3), "Vote denied by Node 02 (term 3)."),
            (new VoteResponseGrantedEvent(2, 2, 3), "Vote granted by Node 02. Total=2/3."),
            (new BecameLeaderEvent(3), "Became leader for term 3."),
            (new OutOfQuorumEvent(2, 5, 3), "Cluster out of quorum: 2/5 (need 3).")
        ];

        foreach (var (raftEvent, expected) in cases)
        {
            // Act
            var message = RaftEventFormatter.Format(raftEvent);

            // Assert
            message.Should().Be(expected);
        }
    }
}
