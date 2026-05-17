namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when an incoming vote request is granted.
/// </summary>
internal sealed record RequestVoteGrantedEvent : RaftEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestVoteGrantedEvent"/> class.
    /// </summary>
    /// <param name="candidateId">Candidate node identifier.</param>
    /// <param name="term">Current term.</param>
    public RequestVoteGrantedEvent(int candidateId, int term)
    {
        CandidateId = DomainEventGuard.RequireNodeId(candidateId, nameof(candidateId), "Candidate id");
        Term = DomainEventGuard.RequireTerm(term, nameof(term));
    }

    /// <summary>
    /// Gets candidate node identifier.
    /// </summary>
    public int CandidateId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public int Term { get; }
}
