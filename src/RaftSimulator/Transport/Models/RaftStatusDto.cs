namespace RaftSimulator.Transport.Models;

/// <summary>
/// Status DTO.
/// </summary>
/// <param name="NodeId">Node identifier.</param>
/// <param name="Term">Current term.</param>
/// <param name="Role">Current role.</param>
/// <param name="LeaderId">Known leader identifier.</param>
internal sealed record RaftStatusDto(int NodeId, int Term, string Role, int? LeaderId);
