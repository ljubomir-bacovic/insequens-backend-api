using FluentAssertions;
using Insequens.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersPipelineBehaviorsInExpectedOrder()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var behaviorRegistrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        behaviorRegistrations.Should().ContainInOrder(
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(OwnershipBehavior<,>));
    }
}
