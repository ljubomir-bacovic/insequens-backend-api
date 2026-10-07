using Insequens.Application.Abstractions;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Persistence;
using Insequens.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Application.Tests.Support;

/// <summary>
/// One SQLite in-memory database per instance, so handler tests run real LINQ translation. Every
/// <see cref="CreateContext"/> call returns a fresh context on the same open connection.
/// </summary>
public sealed class TestDbContextFactory : IDisposable
{
    public static readonly DateTimeOffset StartTime = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDbContextFactory()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public FakeTimeProvider Clock { get; } = new(StartTime);

    public TestCurrentUser CurrentUser { get; } = new();

    public InsequensContext CreateContext(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<InsequensContext>()
        .UseSqlite(_connection)
        .AddInterceptors(new AuditableEntityInterceptor(Clock, CurrentUser))
        .AddInterceptors(interceptors)
        .Options);

    /// <summary>The Application services (MediatR, validators, behaviors) on this database.</summary>
    public ServiceProvider CreateServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IApplicationDbContext>(_ => CreateContext());
        services.AddApplication();
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>Sends the request through the full pipeline (logging, validation, authorization) to its handler.</summary>
    public async Task SendAsync(IRequest request, Action<IServiceCollection>? configure = null)
    {
        await using var services = CreateServices(configure);
        await services.GetRequiredService<IMediator>().Send(request);
    }

    /// <summary>Sends the request through the full pipeline (logging, validation, authorization) to its handler.</summary>
    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        Action<IServiceCollection>? configure = null)
    {
        await using var services = CreateServices(configure);
        return await services.GetRequiredService<IMediator>().Send(request);
    }

    public async Task<ToDoItem> SeedItemAsync(
        Guid userId,
        string name = "Task",
        string? description = null,
        Domain.Types.TaskPriority? priority = null,
        DateOnly? dueDate = null,
        bool isCompleted = false)
    {
        var item = ToDoItem.Create(userId, name, description, priority, dueDate);
        if (isCompleted)
        {
            item.MarkCompleted();
        }

        await using var context = CreateContext();
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        return item;
    }

    public async Task<ToDoItem?> FindItemAsync(Guid itemId)
    {
        await using var context = CreateContext();
        return await context.ToDoItems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == itemId);
    }

    public void Dispose() => _connection.Dispose();
}
