using System.Text.RegularExpressions;
using FluentAssertions;

namespace Insequens.Infrastructure.Tests;

public partial class SourceGuardTests
{
    [Fact]
    public void SourceFiles_DoNotUseDateTimeNow()
    {
        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");

        var offenders = Directory
            .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Where(path => DateTimeNowPattern().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(sourceDirectory, path))
            .ToList();

        offenders.Should().BeEmpty("audit and business timestamps must be UTC; use TimeProvider or DateTime.UtcNow");
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

    [GeneratedRegex(@"\bDateTime\.Now\b")]
    private static partial Regex DateTimeNowPattern();
}
