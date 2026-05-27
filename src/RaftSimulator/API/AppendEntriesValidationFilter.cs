using RaftSimulator.Transport;
using RaftSimulator.Contracts;

namespace RaftSimulator.API;

/// <summary>
/// Validates append-entries endpoint payloads.
/// </summary>
internal sealed class AppendEntriesValidationFilter : RaftRequestValidationFilter<RaftAppendEntriesRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendEntriesValidationFilter"/> class.
    /// </summary>
    public AppendEntriesValidationFilter()
        : base(RaftDtoValidator.TryValidate)
    {
    }
}
