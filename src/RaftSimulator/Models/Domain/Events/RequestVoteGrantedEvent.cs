namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when an incoming vote request is granted.
/// </summary>
/// <param name="CandidateId">Candidate node identifier.</param>
/// <param name="Term">Current term.</param>
internal sealed record RequestVoteGrantedEvent(int CandidateId, int Term) : RaftEvent;
