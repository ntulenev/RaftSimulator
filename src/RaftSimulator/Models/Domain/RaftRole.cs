namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft node role.
/// </summary>
internal enum RaftRole
{
    /// <summary>
    /// Follower role.
    /// </summary>
    Follower,

    /// <summary>
    /// Candidate role.
    /// </summary>
    Candidate,

    /// <summary>
    /// Leader role.
    /// </summary>
    Leader
}
