using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Result of handling a vote response from a peer.
/// </summary>
/// <param name="Events">Events emitted by the decision.</param>
/// <param name="BecameLeader">Value indicating whether the node became leader.</param>
/// <param name="Term">Term associated with follow-up work.</param>
/// <param name="StatusSnapshot">Optional status snapshot to publish.</param>
internal sealed record VoteResponseDecision(
    IReadOnlyList<RaftEvent> Events,
    bool BecameLeader,
    int Term,
    RaftStatus? StatusSnapshot);
