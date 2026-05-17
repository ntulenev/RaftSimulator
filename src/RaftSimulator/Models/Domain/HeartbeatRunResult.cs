namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of a heartbeat broadcast run.
/// </summary>
/// <param name="Responses">Successful append-entries responses.</param>
/// <param name="AcknowledgedPeerIds">Peers that acknowledged the heartbeat.</param>
internal sealed record HeartbeatRunResult(
    IReadOnlyList<RaftAppendEntriesResponse> Responses,
    IReadOnlyList<int> AcknowledgedPeerIds);
