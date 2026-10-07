using System.Xml.Linq;
using FluentAssertions;

namespace Insequens.Infrastructure.Tests;

public class ProjectDependencyTests
{
    [Fact]
    public void ApiProject_PackageReferences_ExcludePersistenceAndMappingPackages()
    {
        var packages = PackageReferences(Path.Combine("src", "Insequens.Api", "Insequens.Api.csproj"));

        packages.Should().NotContain(
            [
                "AutoMapper",
                "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore.InMemory",
                "Microsoft.EntityFrameworkCore.Sqlite",
                "Microsoft.EntityFrameworkCore.SqlServer",
            ],
            "EF providers, Identity stores and AutoMapper belong to Infrastructure, Application or the tests");
    }

    private static IReadOnlyList<string> PackageReferences(string relativeProjectPath)
    {
        var project = XDocument.Load(Path.Combine(RepositoryPaths.FindRepositoryRoot(), relativeProjectPath));

        return project.Descendants("PackageReference")
            .Select(reference => (string?)reference.Attribute("Include") ?? string.Empty)
            .ToList();
    }
}
