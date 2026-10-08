using System.Data.Common;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Insequens.Application.Behaviors;
using Insequens.Application.Authorization;
using Insequens.Application.Abstractions;
using Insequens.Application.Tests.Support;
using Insequens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            typeof(AuthorizationBehavior<,>),
            typeof(IdempotencyBehavior<,>));
    }

    [Fact]
    public async Task AddApplication_ResolvesPipelineBehaviorsAndExecutesThemInExpectedOrder()
    {
        using var database = new TestDbContextFactory();
        var trace = new ExecutionTrace();
        var userId = Guid.NewGuid();
        var item = await database.SeedItemAsync(userId);
        var request = new TestOwnedRequest(userId, item.Id, "example");
        var services = new ServiceCollection();

        services.AddSingleton(trace);
        services.AddSingleton(typeof(ILogger<>), typeof(CapturingLogger<>));
        services.AddScoped<IApplicationDbContext>(_ => database.CreateContext(new QueryTracingInterceptor(trace)));
        services.AddApplication();
        services.AddTransient<IRequestHandler<TestOwnedRequest, string>, TestOwnedRequestHandler>();
        services.AddTransient<IValidator<TestOwnedRequest>, TestOwnedRequestValidator>();

        await using var serviceProvider = services.BuildServiceProvider();

        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TestOwnedRequest, string>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ToArray();

        behaviors.Should().Equal(
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(AuthorizationBehavior<,>));

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(request);

        response.Should().Be("handled:example");
        trace.Steps.Should().Equal(
            "log:Handling TestOwnedRequest",
            "validation",
            "query",
            "handler",
            "log:Handled TestOwnedRequest");
    }

    private static ServiceCollection CreateApplicationServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IApplicationDbContext>());

        return services;
    }

    private sealed class ExecutionTrace
    {
        public List<string> Steps { get; } = [];
    }

    private sealed record TestOwnedRequest(Guid UserId, Guid ResourceId, string Name) : IRequest<string>, IOwned<ToDoItem>;

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

    /// <summary>Records each database read, which is how the ownership check shows up in the trace.</summary>
    private sealed class QueryTracingInterceptor(ExecutionTrace trace) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            trace.Steps.Add("query");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
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
