using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Insequens.Application.Behaviors;
using Insequens.Application.Commands;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Insequens.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatRAndAutoMapperServices()
    {
        var services = CreateApplicationServiceCollection();

        services.AddApplication();

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<ISender>().Should().NotBeNull();
        serviceProvider.GetService<IPublisher>().Should().NotBeNull();
        serviceProvider.GetService<IMapper>().Should().NotBeNull();
        serviceProvider.GetService<IConfigurationProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_RegistersPipelineBehaviorsInExpectedOrderWithoutDuplicates()
    {
        var services = CreateApplicationServiceCollection();

        services.AddApplication();

        var behaviorRegistrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        behaviorRegistrations.Should().Equal(
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(OwnershipBehavior<,>));
    }

    [Fact]
    public async Task AddApplication_ResolvesPipelineBehaviorsAndExecutesThemInExpectedOrder()
    {
        var trace = new ExecutionTrace();
        var request = new TestOwnedRequest(Guid.NewGuid(), Guid.NewGuid(), "example");
        var repository = Substitute.For<IRepository<ToDoItem>>();
        var dataContext = Substitute.For<IDataContext>();
        var services = CreateApplicationServiceCollection(trace, dataContext);

        repository.FindAsync(request.ItemId).Returns(_ =>
        {
            trace.Steps.Add("ownership");
            return Task.FromResult<ToDoItem?>(new ToDoItem
            {
                Id = request.ItemId,
                UserId = request.UserId,
            });
        });
        dataContext.GetRepository<ToDoItem>().Returns(repository);

        services.AddApplication();
        services.AddTransient<IRequestHandler<TestOwnedRequest, string>, TestOwnedRequestHandler>();
        services.AddTransient<IValidator<TestOwnedRequest>, TestOwnedRequestValidator>();

        using var serviceProvider = services.BuildServiceProvider();

        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TestOwnedRequest, string>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ToArray();

        behaviors.Should().Equal(
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(OwnershipBehavior<,>));

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(request);

        response.Should().Be("handled:example");
        trace.Steps.Should().Equal(
            "log:Handling TestOwnedRequest",
            "validation",
            "ownership",
            "handler",
            "log:Handled TestOwnedRequest");
        await repository.Received(1).FindAsync(request.ItemId);
    }

    private static ServiceCollection CreateApplicationServiceCollection(
        ExecutionTrace? trace = null,
        IDataContext? dataContext = null)
    {
        var services = new ServiceCollection();

        if (trace is not null)
        {
            services.AddSingleton(trace);
            services.AddSingleton(typeof(ILogger<>), typeof(CapturingLogger<>));
        }
        else
        {
            services.AddLogging();
        }

        services.AddSingleton(dataContext ?? Substitute.For<IDataContext>());

        return services;
    }

    private sealed class ExecutionTrace
    {
        public List<string> Steps { get; } = [];
    }

    private sealed record TestOwnedRequest(Guid UserId, Guid ItemId, string Name) : IRequest<string>, IOwned;

    private sealed class TestOwnedRequestHandler(ExecutionTrace trace) : IRequestHandler<TestOwnedRequest, string>
    {
        public Task<string> Handle(TestOwnedRequest request, CancellationToken cancellationToken)
        {
            trace.Steps.Add("handler");
            return Task.FromResult($"handled:{request.Name}");
        }
    }

    private sealed class TestOwnedRequestValidator : AbstractValidator<TestOwnedRequest>
    {
        public TestOwnedRequestValidator(ExecutionTrace trace)
        {
            RuleFor(request => request.Name).Custom((_, _) => trace.Steps.Add("validation"));
        }
    }

    private sealed class CapturingLogger<T>(ExecutionTrace trace) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (message.StartsWith("Handled ", StringComparison.Ordinal))
            {
                message = message.Split(" in ", 2, StringSplitOptions.None)[0];
            }

            trace.Steps.Add($"log:{message}");
        }
    }
}
