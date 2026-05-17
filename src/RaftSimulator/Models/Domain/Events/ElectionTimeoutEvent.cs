namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Event emitted when election timeout elapses.
/// </summary>
/// <param name="Term">New election term.</param>
internal sealed record ElectionTimeoutEvent(int Term) : RaftEvent;
