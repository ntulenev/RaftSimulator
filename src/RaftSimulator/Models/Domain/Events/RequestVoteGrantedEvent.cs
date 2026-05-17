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
    public RequestVoteGrantedEvent(CandidateId candidateId, Term term)
    {
        ArgumentNullException.ThrowIfNull(candidateId);
        ArgumentNullException.ThrowIfNull(term);

        CandidateId = candidateId;
        Term = term;
    }

    /// <summary>
    /// Gets candidate node identifier.
    /// </summary>
    public CandidateId CandidateId { get; }

    /// <summary>
    /// Gets current term.
    /// </summary>
    public Term Term { get; }
}
