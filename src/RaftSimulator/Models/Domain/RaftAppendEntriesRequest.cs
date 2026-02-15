namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries request (heartbeat) sent to raft peers.
/// </summary>
internal sealed record RaftAppendEntriesRequest(int Term, int LeaderId);
