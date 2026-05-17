using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Tracks status snapshots already published by term.
/// </summary>
internal sealed class RaftStatusReporter
{
    /// <summary>
    /// Clears published status tracking.
    /// </summary>
    public void Reset() =>
        _lastReportedTerm = null;

    /// <summary>
    /// Gets a status snapshot when it should be published for the first time in a term.
    /// </summary>
    /// <param name="status">Current raft status.</param>
    /// <returns>Status snapshot, or null when no snapshot should be published.</returns>
    public RaftStatus? GetSnapshotToPublish(RaftStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.LeaderId is null ||
            (_lastReportedTerm is not null && status.Term.Value <= _lastReportedTerm.Value.Value))
        {
            return null;
        }

        _lastReportedTerm = status.Term;
        return status;
    }

    private Term? _lastReportedTerm;
}
