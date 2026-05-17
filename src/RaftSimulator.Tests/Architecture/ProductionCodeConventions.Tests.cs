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

    [Fact(DisplayName = "Production namespaces match folders")]
    [Trait("Category", "Architecture")]
    public void ProductionNamespacesMatchFolders()
    {
        // Arrange
        var files = GetProductionFiles();

        // Act
        var violations = files
            .Select(GetNamespaceFolderViolation)
            .Where(static violation => violation is not null)
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact(DisplayName = "Domain models do not depend on outer layers")]
    [Trait("Category", "Architecture")]
    public void DomainModelsDoNotDependOnOuterLayers()
    {
        // Arrange
        var files = GetProductionFiles()
            .Where(static file => file.Contains(
                $"{Path.DirectorySeparatorChar}Models{Path.DirectorySeparatorChar}Domain{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToArray();

        // Act
        var violations = files
            .SelectMany(GetOuterLayerDependencyViolations)
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact(DisplayName = "Abstractions do not depend on implementation layers")]
    [Trait("Category", "Architecture")]
    public void AbstractionsDoNotDependOnImplementationLayers()
    {
        // Arrange
        var files = GetProductionFiles()
            .Where(static file => file.Contains(
                $"{Path.DirectorySeparatorChar}Abstractions{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToArray();

        // Act
        var violations = files
            .SelectMany(GetImplementationLayerDependencyViolations)
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact(DisplayName = "Logic does not depend on transport or host layers")]
    [Trait("Category", "Architecture")]
    public void LogicDoesNotDependOnTransportOrHostLayers()
    {
        // Arrange
        var files = GetProductionFiles()
            .Where(static file => file.Contains(
                $"{Path.DirectorySeparatorChar}Logic{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToArray();

        // Act
        var violations = files
            .SelectMany(GetTransportOrHostLayerDependencyViolations)
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact(DisplayName = "RPC contracts stay in API and transport layers")]
    [Trait("Category", "Architecture")]
    public void TransportDtosStayInApiAndTransportLayers()
    {
        // Arrange
        var files = GetProductionFiles()
            .Where(static file => !IsInsideTopLevelFolder(file, "API"))
            .Where(static file => !IsInsideTopLevelFolder(file, "Transport"))
            .ToArray();

        // Act
        var violations = files
            .SelectMany(GetRpcContractDependencyViolations)
            .ToArray();

        // Assert
        violations.Should().BeEmpty();
    }

    private static string? GetNamespaceFolderViolation(string file)
    {
        var declaredNamespace = File
            .ReadLines(file)
            .Select(static line => NamespaceRegex().Match(line))
            .FirstOrDefault(static match => match.Success)
            ?.Groups["namespace"].Value;

        if (declaredNamespace is null)
        {
            return null;
        }

        var relativeDirectory = Path.GetDirectoryName(GetRelativePath(file));
        var expectedNamespace = string.IsNullOrEmpty(relativeDirectory)
            ? "RaftSimulator"
            : $"RaftSimulator.{relativeDirectory.Replace(Path.DirectorySeparatorChar, '.')}";

        return declaredNamespace == expectedNamespace
            ? null
            : $"{GetRelativePath(file)} declares {declaredNamespace}, expected {expectedNamespace}";
    }

    private static IEnumerable<string> GetRpcContractDependencyViolations(string file)
    {
        return GetUsingDependencyViolations(
            file,
            RpcContractUsingRegex(),
            "depends on RPC contracts outside API or transport layer");
    }

    private static IEnumerable<string> GetTransportOrHostLayerDependencyViolations(string file)
    {
        return GetUsingDependencyViolations(
            file,
            TransportOrHostLayerUsingRegex(),
            "depends on transport or host layer");
    }

    private static IEnumerable<string> GetImplementationLayerDependencyViolations(string file)
    {
        return GetUsingDependencyViolations(
            file,
            ImplementationLayerUsingRegex(),
            "depends on an implementation layer");
    }

    private static IEnumerable<string> GetOuterLayerDependencyViolations(string file)
    {
        return GetUsingDependencyViolations(
            file,
            OuterLayerUsingRegex(),
            "depends on an outer layer");
    }

    private static IEnumerable<string> GetUsingDependencyViolations(
        string file,
        Regex dependencyRegex,
        string message)
    {
        var lines = File.ReadAllLines(file);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (dependencyRegex.IsMatch(line))
            {
                yield return $"{GetRelativePath(file)}:{index + 1} {message}";
            }
        }
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

    private static bool IsInsideTopLevelFolder(string file, string folder)
    {
        var relativePath = GetRelativePath(file);
        return relativePath.StartsWith(
            $"{folder}{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*(internal|public)\s+(sealed\s+|static\s+|abstract\s+|readonly\s+|partial\s+)*((record\s+)?(class|struct)|record|interface|enum)\s+")]
    private static partial Regex TypeDeclarationRegex();

    [GeneratedRegex(@"^using\s+RaftSimulator\.(API|Hosting|Logic|Presentation|Transport)\b")]
    private static partial Regex OuterLayerUsingRegex();

    [GeneratedRegex(@"^using\s+RaftSimulator\.(API|Hosting|Logic|Presentation|Transport)\b")]
    private static partial Regex ImplementationLayerUsingRegex();

    [GeneratedRegex(@"^using\s+RaftSimulator\.(API|Hosting|Presentation|Transport)\b")]
    private static partial Regex TransportOrHostLayerUsingRegex();

    [GeneratedRegex(@"^using\s+RaftSimulator\.Contracts\b")]
    private static partial Regex RpcContractUsingRegex();

    [GeneratedRegex(@"^namespace\s+(?<namespace>[A-Za-z0-9_.]+);$")]
    private static partial Regex NamespaceRegex();
}
