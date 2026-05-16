using System.Text.RegularExpressions;

using FluentAssertions;

namespace RaftSimulator.Tests.Architecture;

public sealed partial class ProductionCodeConventionsTests
{
    [Fact(DisplayName = "Production files contain at most one top-level type")]
    [Trait("Category", "Architecture")]
    public void ProductionFilesContainAtMostOneTopLevelType()
    {
        // Arrange
        var files = GetProductionFiles();

        // Act
        var violations = files
            .Select(file => new
            {
                File = file,
                TypeCount = File.ReadLines(file).Count(line => TypeDeclarationRegex().IsMatch(line))
            })
            .Where(item => item.TypeCount > 1)
            .Select(item => $"{GetRelativePath(item.File)} declares {item.TypeCount} types")
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact(DisplayName = "Production types and public members have XML documentation")]
    [Trait("Category", "Architecture")]
    public void ProductionTypesAndPublicMembersHaveXmlDocumentation()
    {
        // Arrange
        var files = GetProductionFiles();

        // Act
        var violations = files
            .SelectMany(GetMissingXmlDocumentationViolations)
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    private static IEnumerable<string> GetMissingXmlDocumentationViolations(string file)
    {
        var lines = File.ReadAllLines(file);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!RequiresXmlDocumentation(line))
            {
                continue;
            }

            var previousIndex = index - 1;
            while (previousIndex >= 0 && ShouldSkipBeforeDeclaration(lines[previousIndex]))
            {
                previousIndex--;
            }

            if (previousIndex < 0 || !lines[previousIndex].TrimStart().StartsWith("///", StringComparison.Ordinal))
            {
                yield return $"{GetRelativePath(file)}:{index + 1} lacks XML documentation";
            }
        }
    }

    private static bool ShouldSkipBeforeDeclaration(string line)
    {
        var trimmed = line.TrimStart();
        return string.IsNullOrWhiteSpace(trimmed)
            || trimmed.StartsWith('[');
    }

    private static bool RequiresXmlDocumentation(string line)
    {
        if (TypeDeclarationRegex().IsMatch(line))
        {
            return true;
        }

        var trimmed = line.TrimStart();
        return trimmed.StartsWith("public ", StringComparison.Ordinal)
            && !TypeDeclarationRegex().IsMatch(line);
    }

    private static string[] GetProductionFiles()
    {
        var projectDirectory = GetProductionProjectDirectory();
        return
        [
            .. Directory
            .GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.EndsWith("Program.cs", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
        ];
    }

    private static string GetProductionProjectDirectory() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "RaftSimulator"));

    private static string GetRelativePath(string file) =>
        Path.GetRelativePath(GetProductionProjectDirectory(), file);

    [GeneratedRegex(@"^\s*(internal|public)\s+(sealed\s+|static\s+|abstract\s+|readonly\s+|partial\s+)*((record\s+)?(class|struct)|record|interface|enum)\s+")]
    private static partial Regex TypeDeclarationRegex();
}
