namespace RaftSimulator.Logic;

/// <summary>
/// Result of handling an append entries response from a peer.
/// </summary>
/// <param name="LogLines">Log lines for the decision.</param>
internal sealed record AppendEntriesResponseDecision(IReadOnlyList<string> LogLines);
