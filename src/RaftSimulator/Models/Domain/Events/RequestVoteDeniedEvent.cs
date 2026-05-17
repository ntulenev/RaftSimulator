namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when an incoming vote request is denied.
/// </summary>
internal sealed record RequestVoteDeniedEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestVoteDeniedEvent"/> class.
    /// </summary>
    /// <param name="candidateId">Candidate node identifier.</param>
    /// <param name="term">Term used in the log message.</param>
    public RequestVoteDeniedEvent(int candidateId, int term)
    {
        CandidateId = DomainEventGuard.RequireNodeId(candidateId, nameof(candidateId), "Candidate id");
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets candidate node identifier.
    /// </summary>
    public int CandidateId { get; }

    /// <summary>
    /// Gets term used in the log message.
    /// </summary>
    public int Term { get; }
}
