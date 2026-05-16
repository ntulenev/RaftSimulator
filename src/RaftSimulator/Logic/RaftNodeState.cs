using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Mutable state owned by a raft node state machine.
/// </summary>
internal sealed class RaftNodeState
{
    public RaftRole Role { get; set; }

    public int CurrentTerm { get; set; }

    public int? VotedFor { get; set; }

    public int? LeaderId { get; set; }

    public int VotesReceived { get; set; }

    public DateTimeOffset LeaderSince { get; set; }

    public DateTimeOffset NextElectionDeadline { get; set; }

    public DateTimeOffset NextHeartbeatAt { get; set; }

    public int LastReportedTerm { get; set; } = -1;

    public Dictionary<int, DateTimeOffset> LastHeartbeatAckAt { get; } = [];
}
