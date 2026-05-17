namespace RaftSimulator.Models.Domain;

/// <summary>
/// Vote response from raft peers.
/// </summary>
internal sealed record RaftVoteResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftVoteResponse"/> class.
    /// </summary>
    /// <param name="term">Responder term.</param>
    /// <param name="fromId">Responder node identifier.</param>
    /// <param name="granted">Value indicating whether the vote was granted.</param>
    public RaftVoteResponse(Term term, FromId fromId, bool granted)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(fromId);

        Term = term;
        FromId = fromId;
        Granted = granted;
    }

    /// <summary>
    /// Gets responder term.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets responder node identifier.
    /// </summary>
    public FromId FromId { get; }

    /// <summary>
    /// Gets a value indicating whether the vote was granted.
    /// </summary>
    public bool Granted { get; }

    /// <summary>
    /// Determines whether this response discovered a higher term.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <returns>True when response term is newer.</returns>
    public bool HasHigherTermThan(Term currentTerm) => Term.IsNewerThan(currentTerm);

    /// <summary>
    /// Determines whether this response belongs to the current election term.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <returns>True when response term equals current term.</returns>
    public bool IsForTerm(Term currentTerm)
    {
        ArgumentNullException.ThrowIfNull(currentTerm);

        return Term == currentTerm;
    }
}
