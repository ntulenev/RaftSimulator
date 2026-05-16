namespace RaftSimulator.Logic.Events;

/// <summary>
/// Event emitted when a peer grants a vote.
/// </summary>
/// <param name="FromId">Peer node identifier.</param>
/// <param name="TotalVotes">Total votes received.</param>
/// <param name="Majority">Majority threshold.</param>
internal sealed record VoteResponseGrantedEvent(int FromId, int TotalVotes, int Majority) : RaftEvent;
