namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries request (heartbeat) sent to raft peers.
/// </summary>
internal sealed record RaftAppendEntriesRequest(Term Term, LeaderId LeaderId);
