namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of a heartbeat broadcast run.
/// </summary>
internal sealed record HeartbeatRunResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatRunResult"/> class.
    /// </summary>
    /// <param name="responses">Successful append-entries responses.</param>
    /// <param name="acknowledgedPeerIds">Peers that acknowledged the heartbeat.</param>
    public HeartbeatRunResult(
        IReadOnlyList<RaftAppendEntriesResponse> responses,
        IReadOnlyList<int> acknowledgedPeerIds)
    {
        ArgumentNullException.ThrowIfNull(responses);
        ArgumentNullException.ThrowIfNull(acknowledgedPeerIds);

        if (responses.Any(static response => response is null))
        {
            throw new ArgumentException("Responses must not contain null items.", nameof(responses));
        }

        if (acknowledgedPeerIds.Any(static peerId => peerId < 1))
        {
            throw new ArgumentOutOfRangeException(nameof(acknowledgedPeerIds), acknowledgedPeerIds, "Peer ids must be >= 1.");
        }

        Responses = responses;
        AcknowledgedPeerIds = acknowledgedPeerIds;
    }

    /// <summary>
    /// Gets successful append-entries responses.
    /// </summary>
    public IReadOnlyList<RaftAppendEntriesResponse> Responses { get; }

    /// <summary>
    /// Gets peers that acknowledged the heartbeat.
    /// </summary>
    public IReadOnlyList<int> AcknowledgedPeerIds { get; }
}
