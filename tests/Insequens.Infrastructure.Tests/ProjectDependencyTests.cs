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

    [Fact]
    public void ContractsProject_ReferencesNothing()
    {
        var project = LoadProject(Path.Combine("src", "Insequens.Contracts", "Insequens.Contracts.csproj"));

        project.Descendants("ProjectReference").Should().BeEmpty("clients consume the contract without the server");
        project.Descendants("PackageReference").Should().BeEmpty("clients consume the contract without the server");
        project.Descendants("FrameworkReference").Should().BeEmpty("clients consume the contract without the server");
    }

    [Fact]
    public void ContractsAssembly_ReferencesOnlyTheRuntime()
    {
        var references = typeof(Contracts.V1.PaginatedResult<>).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);

        references.Should().OnlyContain(name => name == "netstandard" || name == "System" || name.StartsWith("System.", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainProject_ReferencesNothing()
    {
        var project = LoadProject(Path.Combine("src", "Insequens.Domain", "Insequens.Domain.csproj"));

        project.Descendants("ProjectReference").Should().BeEmpty();
        project.Descendants("PackageReference").Should().BeEmpty();
    }

    private static IReadOnlyList<string> PackageReferences(string relativeProjectPath) =>
        LoadProject(relativeProjectPath).Descendants("PackageReference")
            .Select(reference => (string?)reference.Attribute("Include") ?? string.Empty)
            .ToList();

    private static XDocument LoadProject(string relativeProjectPath) =>
        XDocument.Load(Path.Combine(RepositoryPaths.FindRepositoryRoot(), relativeProjectPath));
}
