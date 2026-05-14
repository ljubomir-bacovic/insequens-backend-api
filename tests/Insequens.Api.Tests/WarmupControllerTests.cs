using FluentAssertions;
using Insequens.Api.Controllers;
using Insequens.Domain.Data;
using Insequens.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Insequens.Api.Tests;

public class WarmupControllerTests
{
    [Fact]
    public async Task Get_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        await using var context = CreateReachableContext();
        await context.Database.EnsureCreatedAsync();
        var controller = new WarmupController(context);

        var result = await controller.Get(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("Healthy");
    }

    [Fact]
    public async Task Get_WhenDatabaseIsUnavailable_ReturnsServiceUnavailable()
    {
        await using var context = CreateUnavailableContext();
        var controller = new WarmupController(context);

        var result = await controller.Get(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        objectResult.Value.Should().Be("Database unavailable");
    }

    private static InsequensContext CreateReachableContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestInsequensContext(options);
    }

    private static InsequensContext CreateUnavailableContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=WarmupControllerTests;Connect Timeout=1;Encrypt=False;TrustServerCertificate=True")
            .Options;

        return new TestInsequensContext(options);
    }

    private sealed class TestInsequensContext : InsequensContext
    {
        [SetsRequiredMembers]
        public TestInsequensContext(DbContextOptions<InsequensContext> options) : base(options)
        {
            ToDoItems = Set<ToDoItem>();
        }
    }
}
