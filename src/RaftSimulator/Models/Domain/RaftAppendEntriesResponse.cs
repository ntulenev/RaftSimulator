namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries response from raft peers.
/// </summary>
internal sealed record RaftAppendEntriesResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftAppendEntriesResponse"/> class.
    /// </summary>
    /// <param name="term">Responder term.</param>
    /// <param name="fromId">Responder node identifier.</param>
    /// <param name="success">Value indicating whether append entries was accepted.</param>
    public RaftAppendEntriesResponse(Term term, FromId fromId, bool success)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(fromId);

        Term = term;
        FromId = fromId;
        Success = success;
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
    /// Gets a value indicating whether append entries was accepted.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Determines whether this response discovered a higher term.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <returns>True when response term is newer.</returns>
    public bool HasHigherTermThan(Term currentTerm) => Term.IsNewerThan(currentTerm);
}
