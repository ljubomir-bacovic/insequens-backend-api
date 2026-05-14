using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Insequens.Application.Behaviors;
using Insequens.Application.Commands;
using Insequens.Domain;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using Insequens.Domain.ServiceContracts;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Api.Tests;

public class ProgramStartupTests
{
    [Fact]
    public void Startup_RegistersLegacyAndApplicationServices()
    {
        using var factory = new InsequensApiFactory();
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        serviceProvider.GetRequiredService<IToDoItemService>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ISender>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IPublisher>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IMapper>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IConfigurationProvider>().Should().NotBeNull();
    }

    [Fact]
    public async Task Startup_RegistersApplicationPipelineBehaviorsWithoutBreakingLegacyServices()
    {
        var trace = new ExecutionTrace();
        var request = new TestOwnedRequest(Guid.NewGuid(), Guid.NewGuid(), "example");
        using var factory = new InsequensApiFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(trace);
                    services.AddScoped<IDataContext>(_ => new TestDataContext(request, trace));
                    services.AddTransient<IRequestHandler<TestOwnedRequest, string>, TestOwnedRequestHandler>();
                    services.AddTransient<IValidator<TestOwnedRequest>, TestOwnedRequestValidator>();
                });
            });
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

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
        trace.Steps.Should().Equal("validation", "ownership", "handler");
        serviceProvider.GetRequiredService<IToDoItemService>().Should().NotBeNull();
    }

    private sealed class InsequensApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            builder.UseEnvironment("Development");
        }
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

    private sealed class TestDataContext(TestOwnedRequest request, ExecutionTrace trace) : IDataContext
    {
        public void Dispose()
        {
        }

        public IRepository<T> GetRepository<T>()
            where T : class, IEntity
        {
            if (typeof(T) != typeof(ToDoItem))
            {
                throw new InvalidOperationException($"Unexpected repository type: {typeof(T).Name}");
            }

            return (IRepository<T>)(object)new TestToDoItemRepository(request, trace);
        }

        public void SaveChanges()
        {
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class TestToDoItemRepository(TestOwnedRequest request, ExecutionTrace trace) : IRepository<ToDoItem>
    {
        public void AddOrUpdate(ToDoItem entity, bool? isNew = null) => throw new NotSupportedException();

        public IQueryable<ToDoItem> AsQueryable(params Expression<Func<ToDoItem, object>>[]? includeExpressions) => throw new NotSupportedException();

        public void AddOrUpdate(IEnumerable<ToDoItem> entities, bool? isNew = null) => throw new NotSupportedException();

        public ToDoItem? Find(params object[] keyValues) => throw new NotSupportedException();

        public Task<ToDoItem?> FindAsync(params object[] keyValues)
        {
            trace.Steps.Add("ownership");

            return Task.FromResult<ToDoItem?>(new ToDoItem
            {
                Id = request.ItemId,
                UserId = request.UserId,
            });
        }

        public void Remove(ToDoItem entity) => throw new NotSupportedException();

        public void Remove(IEnumerable<ToDoItem> entities) => throw new NotSupportedException();

        public void Clone(ToDoItem oldEntity, ref ToDoItem newEntity) => throw new NotSupportedException();
    }
}
