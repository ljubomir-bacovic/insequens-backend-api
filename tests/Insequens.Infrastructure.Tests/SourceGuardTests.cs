using System.Text.RegularExpressions;
using FluentAssertions;

namespace Insequens.Infrastructure.Tests;

public partial class SourceGuardTests
{
    private static readonly string[] LocalTimeTypeNames = ["DateTime", "DateTimeOffset"];

    [Fact]
    public void SourceFiles_WhenScanned_DoNotUseLocalNow()
    {
        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");
        var sources = Directory
            .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .ToDictionary(path => Path.GetRelativePath(sourceDirectory, path), File.ReadAllText);

        var projectAliases = sources.Values.SelectMany(FindAliases).ToArray();
        var projectHasStaticImport = sources.Values.Any(source => StaticImportPattern().IsMatch(source));

        var offenders = sources
            .Where(source => UsesLocalNow(source.Value, projectAliases, projectHasStaticImport))
            .Select(source => source.Key)
            .ToList();

        offenders.Should().BeEmpty("timestamps must be UTC; use an injected TimeProvider instead of DateTime.Now or DateTimeOffset.Now");
    }

    [Theory]
    [InlineData("var now = DateTime.Now;")]
    [InlineData("var now = DateTime . Now;")]
    [InlineData("var now = System.DateTime.Now;")]
    [InlineData("var now = global::System.DateTime.Now;")]
    [InlineData("var now = DateTimeOffset.Now;")]
    [InlineData("using LocalClock = System.DateTime;\nvar now = LocalClock.Now;")]
    [InlineData("global using LocalClock = global::System.DateTimeOffset;\nvar now = LocalClock . Now;")]
    [InlineData("using static System.DateTime;\nvar now = Now;")]
    public void UsesLocalNow_WithLocalTimeAccess_ReturnsTrue(string source)
    {
        UsesLocalNow(source, [], projectHasStaticImport: false).Should().BeTrue();
    }

    [Theory]
    [InlineData("var now = DateTime.UtcNow;")]
    [InlineData("var now = DateTimeOffset.UtcNow;")]
    [InlineData("var now = timeProvider.GetUtcNow().UtcDateTime;")]
    [InlineData("var now = clock.Now;")]
    [InlineData("var nowhere = Nowhere;")]
    [InlineData("using static System.Math;\nvar now = Now;")]
    public void UsesLocalNow_WithUtcOrUnrelatedCode_ReturnsFalse(string source)
    {
        UsesLocalNow(source, [], projectHasStaticImport: false).Should().BeFalse();
    }

    [Fact]
    public void UsesLocalNow_WithAliasDeclaredInAnotherFile_ReturnsTrue()
    {
        UsesLocalNow("var now = LocalClock.Now;", ["LocalClock"], projectHasStaticImport: false).Should().BeTrue();
    }

    [Fact]
    public void UsesLocalNow_WithStaticImportInAnotherFile_ReturnsTrue()
    {
        UsesLocalNow("var now = Now;", [], projectHasStaticImport: true).Should().BeTrue();
    }

    private static bool UsesLocalNow(string source, IEnumerable<string> projectAliases, bool projectHasStaticImport)
    {
        var typeNames = LocalTimeTypeNames
            .Concat(FindAliases(source))
            .Concat(projectAliases)
            .Distinct()
            .Select(Regex.Escape);
        var qualifiedNow = new Regex($@"\b(?:{string.Join("|", typeNames)})\s*\.\s*Now\b");

        if (qualifiedNow.IsMatch(source))
        {
            return true;
        }

        var hasStaticImport = projectHasStaticImport || StaticImportPattern().IsMatch(source);
        return hasStaticImport && UnqualifiedNowPattern().IsMatch(source);
    }

    private static IEnumerable<string> FindAliases(string source) =>
        AliasPattern().Matches(source).Select(match => match.Groups["alias"].Value);

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Contains("bin") || segments.Contains("obj");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Insequens.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Insequens.sln not found above the test output directory.");
    }

    [GeneratedRegex(@"\busing\s+(?<alias>\w+)\s*=\s*(?:global::)?(?:System\s*\.\s*)?DateTime(?:Offset)?\s*;")]
    private static partial Regex AliasPattern();

    [GeneratedRegex(@"\busing\s+static\s+(?:global::)?(?:System\s*\.\s*)?DateTime(?:Offset)?\s*;")]
    private static partial Regex StaticImportPattern();

    [GeneratedRegex(@"(?<![.\w]\s*)\bNow\b")]
    private static partial Regex UnqualifiedNowPattern();
}
