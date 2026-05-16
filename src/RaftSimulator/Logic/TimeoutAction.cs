namespace RaftSimulator.Logic;

/// <summary>
/// Describes work to run after a raft timeout.
/// </summary>
/// <param name="Type">Timeout action type.</param>
/// <param name="Term">Term associated with the action.</param>
/// <param name="LogLine">Log line for the action.</param>
internal sealed record TimeoutAction(TimeoutActionType Type, int Term, string LogLine);
