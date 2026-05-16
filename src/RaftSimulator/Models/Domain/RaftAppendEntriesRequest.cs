namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries request (heartbeat) sent to raft peers.
/// </summary>
/// <param name="Term">Leader term.</param>
/// <param name="LeaderId">Leader node identifier.</param>
internal sealed record RaftAppendEntriesRequest(Term Term, LeaderId LeaderId);
