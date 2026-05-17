namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote request sent to raft peers.
/// </summary>
internal sealed record RaftVoteRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftVoteRequest"/> class.
    /// </summary>
    /// <param name="term">Candidate term.</param>
    /// <param name="candidateId">Candidate node identifier.</param>
    public RaftVoteRequest(Term term, CandidateId candidateId)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(candidateId);

        Term = term;
        CandidateId = candidateId;
    }

    /// <summary>
    /// Gets candidate term.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets candidate node identifier.
    /// </summary>
    public CandidateId CandidateId { get; }

    /// <summary>
    /// Determines whether this request is stale for the current term.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <returns>True when the request term is older.</returns>
    public bool IsStaleFor(Term currentTerm) => Term.IsOlderThan(currentTerm);

    /// <summary>
    /// Determines whether this request advances the current term.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <returns>True when the request term is newer.</returns>
    public bool AdvancesTerm(Term currentTerm) => Term.IsNewerThan(currentTerm);

    /// <summary>
    /// Determines whether this request can be granted by the current node state.
    /// </summary>
    /// <param name="role">Current role.</param>
    /// <param name="votedFor">Candidate already voted for in this term.</param>
    /// <returns>True when this vote can be granted.</returns>
    public bool CanBeGrantedBy(RaftRole role, CandidateId? votedFor) =>
        role != RaftRole.Leader && (votedFor is null || votedFor == CandidateId);
}
