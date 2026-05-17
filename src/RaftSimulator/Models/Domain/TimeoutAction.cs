using RaftSimulator.Logic.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Describes work to run after a raft timeout.
/// </summary>
/// <param name="Type">Timeout action type.</param>
/// <param name="Term">Term associated with the action.</param>
/// <param name="Events">Events emitted by the action.</param>
internal sealed record TimeoutAction(
    TimeoutActionType Type,
    int Term,
    IReadOnlyList<RaftEvent> Events);
