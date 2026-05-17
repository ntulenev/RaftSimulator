using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Presentation;

/// <summary>
/// Formats raft events as log messages.
/// </summary>
internal static class RaftEventFormatter
{
    /// <summary>
    /// Formats a raft event.
    /// </summary>
    /// <param name="raftEvent">Raft event.</param>
    /// <returns>Log message.</returns>
    public static string Format(RaftEvent raftEvent)
    {
        ArgumentNullException.ThrowIfNull(raftEvent);

        return raftEvent switch
        {
            RequestVoteDeniedEvent e => $"Denied vote to Node {e.CandidateId:00} (term {e.Term}).",
            RequestVoteGrantedEvent e => $"Granted vote to Node {e.CandidateId:00} (term {e.Term}).",
            BecameFollowerEvent e => FormatBecameFollower(e),
            HeartbeatIgnoredEvent e => $"Ignored heartbeat from Node {e.LeaderId:00} (term {e.Term}).",
            HeartbeatReceivedEvent e => $"Heartbeat from Node {e.LeaderId:00} (term {e.Term}).",
            LeaderHeartbeatEvent => "Leader heartbeat.",
            ElectionTimeoutEvent e => $"Election timeout. Term {e.Term}, becoming candidate.",
            HigherTermDiscoveredEvent e => $"Discovered higher term {e.Term} from Node {e.FromId:00}.",
            VoteResponseDeniedEvent e => $"Vote denied by Node {e.FromId:00} (term {e.Term}).",
            VoteResponseGrantedEvent e => $"Vote granted by Node {e.FromId:00}. Total={e.TotalVotes}/{e.Majority}.",
            BecameLeaderEvent e => $"Became leader for term {e.Term}.",
            OutOfQuorumEvent e => $"Cluster out of quorum: {e.Reachable}/{e.Total} (need {e.Needed}).",
            _ => throw new ArgumentOutOfRangeException(nameof(raftEvent), raftEvent, "Unknown event.")
        };
    }

    private static string FormatBecameFollower(BecameFollowerEvent raftEvent)
    {
        var leaderText = raftEvent.LeaderId is null ? "unknown" : $"Node {raftEvent.LeaderId:00}";
        return $"Became follower for term {raftEvent.Term} (leader {leaderText}).";
    }
}
