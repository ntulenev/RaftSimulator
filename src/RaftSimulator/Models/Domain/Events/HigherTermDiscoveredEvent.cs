namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when a peer response carries a higher term.
/// </summary>
/// <param name="Term">Higher term.</param>
/// <param name="FromId">Peer node identifier.</param>
internal sealed record HigherTermDiscoveredEvent(int Term, int FromId) : RaftEvent;
