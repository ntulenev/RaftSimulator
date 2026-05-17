namespace RaftSimulator.Models.Domain;

/// <summary>
/// Append entries request (heartbeat) sent to raft peers.
/// </summary>
internal sealed record RaftAppendEntriesRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftAppendEntriesRequest"/> class.
    /// </summary>
    /// <param name="term">Leader term.</param>
    /// <param name="leaderId">Leader node identifier.</param>
    public RaftAppendEntriesRequest(Term term, LeaderId leaderId)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(leaderId);

        Term = term;
        LeaderId = leaderId;
    }

    /// <summary>
    /// Gets leader term.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets leader node identifier.
    /// </summary>
    public LeaderId LeaderId { get; }

    /// <summary>
    /// Determines whether this request is stale for the current term.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <returns>True when the request term is older.</returns>
    public bool IsStaleFor(Term currentTerm) => Term.IsOlderThan(currentTerm);

    /// <summary>
    /// Determines whether this request requires the local node to become follower.
    /// </summary>
    /// <param name="currentTerm">Current term.</param>
    /// <param name="currentRole">Current role.</param>
    /// <returns>True when the request should make this node follow the sender.</returns>
    public bool ShouldMakeFollower(Term currentTerm, RaftRole currentRole) =>
        Term.IsNewerThan(currentTerm) || currentRole != RaftRole.Follower;
}
