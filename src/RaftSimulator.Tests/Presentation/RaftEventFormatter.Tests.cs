using FluentAssertions;

using RaftSimulator.Models.Domain.Events;
using RaftSimulator.Models.Domain;
using RaftSimulator.Presentation;

namespace RaftSimulator.Tests.Presentation;

public sealed class RaftEventFormatterTests
{
    [Fact(DisplayName = "Format returns expected log messages")]
    [Trait("Category", "Unit")]
    public void FormatReturnsExpectedLogMessages()
    {
        // Arrange
        (RaftEvent Event, string Expected)[] cases =
        [
            (new RequestVoteDeniedEvent(new CandidateId(2), new Term(1)), "Denied vote to Node 02 (term 1)."),
            (new RequestVoteGrantedEvent(new CandidateId(2), new Term(1)), "Granted vote to Node 02 (term 1)."),
            (new BecameFollowerEvent(new Term(3), null), "Became follower for term 3 (leader unknown)."),
            (new BecameFollowerEvent(new Term(3), new LeaderId(2)), "Became follower for term 3 (leader Node 02)."),
            (new HeartbeatIgnoredEvent(new LeaderId(2), new Term(1)), "Ignored heartbeat from Node 02 (term 1)."),
            (new HeartbeatReceivedEvent(new LeaderId(2), new Term(3)), "Heartbeat from Node 02 (term 3)."),
            (new LeaderHeartbeatEvent(), "Leader heartbeat."),
            (new ElectionTimeoutEvent(new Term(4)), "Election timeout. Term 4, becoming candidate."),
            (new HigherTermDiscoveredEvent(new Term(5), new FromId(2)), "Discovered higher term 5 from Node 02."),
            (new VoteResponseDeniedEvent(new FromId(2), new Term(3)), "Vote denied by Node 02 (term 3)."),
            (new VoteResponseGrantedEvent(new FromId(2), 2, 3), "Vote granted by Node 02. Total=2/3."),
            (new BecameLeaderEvent(new Term(3)), "Became leader for term 3."),
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

    [Fact(DisplayName = "Format throws for unknown event type")]
    [Trait("Category", "Unit")]
    public void FormatWhenEventIsUnknownThrows()
    {
        // Act
        var act = () => RaftEventFormatter.Format(new UnknownRaftEvent());

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed record UnknownRaftEvent : RaftEvent;
}
