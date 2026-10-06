using System.Text.RegularExpressions;
using FluentAssertions;

namespace Insequens.Infrastructure.Tests;

public partial class SourceGuardTests
{
    [Fact]
    public void SourceFiles_WhenScanned_DoNotUseLocalNow()
    {
        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");

        var offenders = Directory
            .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Where(path => LocalNowPattern().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(sourceDirectory, path))
            .ToList();

        offenders.Should().BeEmpty("timestamps must be UTC; use an injected TimeProvider instead of DateTime.Now or DateTimeOffset.Now");
    }

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

    [GeneratedRegex(@"\bDateTime(?:Offset)?\s*\.\s*Now\b")]
    private static partial Regex LocalNowPattern();
}
