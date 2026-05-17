namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when an incoming vote request is denied.
/// </summary>
/// <param name="CandidateId">Candidate node identifier.</param>
/// <param name="Term">Term used in the log message.</param>
internal sealed record RequestVoteDeniedEvent(int CandidateId, int Term) : RaftEvent;
