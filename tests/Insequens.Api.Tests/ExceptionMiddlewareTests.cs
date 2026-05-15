using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Insequens.Api.Tests;

public class ExceptionMiddlewareTests
{
    private const string ModelLevelErrorKey = "";

    [Fact]
    public async Task Invoke_WhenNoExceptionIsThrown_PassesThroughResponse()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(async httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
            await httpContext.Response.WriteAsync("ok");
        });

        await middleware.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        (await ReadResponseBodyAsync(context)).Should().Be("ok");
    }

    [Fact]
    public async Task Invoke_WhenToDoItemNotFoundExceptionIsThrown_Returns404ProblemDetails()
    {
        var itemId = Guid.NewGuid();
        var exception = new ToDoItemNotFoundException(itemId);
        var context = await InvokeMiddlewareAsync(_ => throw exception);
        var responseBody = await ReadResponseBodyAsync(context);
        using var json = JsonDocument.Parse(responseBody);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().Be("application/problem+json");
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        json.RootElement.GetProperty("title").GetString().Should().Be($"To Do item for id {itemId} not found.");
        json.RootElement.GetProperty("type").GetString().Should().Be("Error");
        responseBody.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task Invoke_WhenResourceForbiddenExceptionIsThrown_Returns403ProblemDetails()
    {
        var itemId = Guid.NewGuid();
        var exception = new ResourceForbiddenException(itemId);
        var context = await InvokeMiddlewareAsync(_ => throw exception);
        var responseBody = await ReadResponseBodyAsync(context);
        using var json = JsonDocument.Parse(responseBody);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        context.Response.ContentType.Should().Be("application/problem+json");
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status403Forbidden);
        json.RootElement.GetProperty("title").GetString().Should().Be("Access denied.");
        json.RootElement.GetProperty("type").GetString().Should().Be("Error");
        responseBody.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task Invoke_WhenFluentValidationExceptionIsThrown_ReturnsGroupedProblemDetails()
    {
        var exception = new ValidationException(
            "Do not leak this exception message.",
            [
                new ValidationFailure("Name", "Name is required."),
                new ValidationFailure("Name", "Name must be at least 3 characters."),
                new ValidationFailure("Priority", "Priority must be between 0 and 3.")
            ]);
        var context = await InvokeMiddlewareAsync(_ => throw exception);
        var responseBody = await ReadResponseBodyAsync(context);
        using var json = JsonDocument.Parse(responseBody);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");
        json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status400BadRequest);
        json.RootElement.GetProperty("title").GetString().Should().Be("Validation failed.");
        json.RootElement.GetProperty("detail").GetString()!.Split("; ").Should().BeEquivalentTo(
            "Name is required.",
            "Name must be at least 3 characters.",
            "Priority must be between 0 and 3.");
        json.RootElement.GetProperty("type").GetString().Should().Be("ValidationError");
        json.RootElement.GetProperty("errors").GetProperty("Name").EnumerateArray().Select(item => item.GetString()).Should().Equal(
            "Name is required.",
            "Name must be at least 3 characters.");
        json.RootElement.GetProperty("errors").GetProperty("Priority").EnumerateArray().Select(item => item.GetString()).Should().Equal("Priority must be between 0 and 3.");
        responseBody.Should().NotContain("Do not leak this exception message.");
        responseBody.Should().NotContain("FluentValidation.ValidationException");
        responseBody.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task Invoke_WhenFluentValidationExceptionHasNoFailures_ReturnsEmptyErrorsObject()
    {
        var exception = new ValidationException("Do not leak this exception message.", []);
        var context = await InvokeMiddlewareAsync(_ => throw exception);
        var responseBody = await ReadResponseBodyAsync(context);
        using var json = JsonDocument.Parse(responseBody);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        json.RootElement.GetProperty("detail").GetString().Should().BeEmpty();
        json.RootElement.GetProperty("errors").EnumerateObject().Should().BeEmpty();
        responseBody.Should().NotContain("Do not leak this exception message.");
    }

    [Fact]
    public async Task Invoke_WhenFluentValidationExceptionContainsModelLevelError_UsesEmptyPropertyNameKey()
    {
        var exception = new ValidationException(
            "Do not leak this exception message.",
            [new ValidationFailure(ModelLevelErrorKey, "A general validation failure occurred.")]);
        var context = await InvokeMiddlewareAsync(_ => throw exception);
        var responseBody = await ReadResponseBodyAsync(context);
        using var json = JsonDocument.Parse(responseBody);

        json.RootElement.GetProperty("detail").GetString().Should().Be("A general validation failure occurred.");
        json.RootElement.GetProperty("errors").GetProperty(ModelLevelErrorKey).EnumerateArray().Select(item => item.GetString()).Should().Equal("A general validation failure occurred.");
        responseBody.Should().NotContain("Do not leak this exception message.");
    }

    private static ExceptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ExceptionMiddleware(next, new TestWebHostEnvironment(), NullLogger<ExceptionMiddleware>.Instance);
    }

    private static async Task<HttpContext> InvokeMiddlewareAsync(RequestDelegate next)
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(next);

        await middleware.Invoke(context);

        return context;
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);

        return await reader.ReadToEndAsync();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = nameof(Insequens);

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = Environments.Production;

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
