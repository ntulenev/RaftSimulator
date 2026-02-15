namespace RaftSimulator.Models.Domain;

/// <summary>
/// Snapshot of raft node state.
/// </summary>
internal sealed record RaftStatus(NodeId NodeId, Term Term, RaftRole Role, LeaderId? LeaderId);
