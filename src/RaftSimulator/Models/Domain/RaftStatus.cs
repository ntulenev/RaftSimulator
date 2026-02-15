namespace RaftSimulator.Models.Domain;

/// <summary>
/// Snapshot of raft node state.
/// </summary>
internal sealed record RaftStatus(int NodeId, int Term, RaftRole Role, int? LeaderId);
