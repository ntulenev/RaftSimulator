using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace RaftSimulator.Models.Configuration;

/// <summary>
/// Validates raft configuration options.
/// </summary>
internal sealed class RaftOptionsValidator : IValidateOptions<RaftOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RaftOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = GetFailures(options);
        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Gets validation failure messages.
    /// </summary>
    /// <param name="options">Options to validate.</param>
    /// <returns>Validation failure messages.</returns>
    public static IReadOnlyList<string> GetFailures(RaftOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        failures.AddRange(GetDataAnnotationFailures(options));

        if (options.MaxElectionSeconds < options.MinElectionSeconds)
        {
            failures.Add("Raft:MaxElectionSeconds must be >= Raft:MinElectionSeconds.");
        }

        if (options.MaxNetworkDelaySeconds < options.MinNetworkDelaySeconds)
        {
            failures.Add("Raft:MaxNetworkDelaySeconds must be >= Raft:MinNetworkDelaySeconds.");
        }

        return failures;
    }

    private static IEnumerable<string> GetDataAnnotationFailures(RaftOptions options)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);
        if (Validator.TryValidateObject(
            options,
            validationContext,
            validationResults,
            validateAllProperties: true))
        {
            return [];
        }

        return validationResults
            .Select(static result => result.ErrorMessage)
            .Where(static message => message is not null)
            .Cast<string>();
    }
}
