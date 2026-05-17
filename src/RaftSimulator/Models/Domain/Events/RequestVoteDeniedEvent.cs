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
    public RequestVoteDeniedEvent(CandidateId candidateId, Term term)
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
    /// Gets term used in the log message.
    /// </summary>
    public Term Term { get; }
}
