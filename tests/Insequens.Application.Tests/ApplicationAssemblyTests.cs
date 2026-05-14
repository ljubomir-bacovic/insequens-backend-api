using System.Reflection;
using FluentAssertions;
using Insequens.Application;

namespace Insequens.Application.Tests;

public class ApplicationAssemblyTests
{
    [Fact]
    public void ApplicationAssembly_CanBeLoaded()
    {
        var assembly = Assembly.Load("Insequens.Application");

        assembly.Should().BeSameAs(typeof(DependencyInjection).Assembly);
    }
}
