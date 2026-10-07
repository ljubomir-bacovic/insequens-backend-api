using System.Net;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Api.Tests.ErrorHandling;

public class ProblemDetailsTests
{
    private const string ProblemJson = "application/problem+json";

    public static TheoryData<string, HttpStatusCode, string, string> Exceptions => new()
    {
        { "not-found", HttpStatusCode.NotFound, "urn:insequens:error:not-found", $"ToDoItem for id {ThrowingTestController.ResourceId} not found." },
        { "forbidden", HttpStatusCode.Forbidden, "urn:insequens:error:forbidden", "Access denied." },
        { "authentication", HttpStatusCode.Unauthorized, "urn:insequens:error:authentication-failed", "Authentication failed." },
        { "email-confirmation", HttpStatusCode.BadRequest, "urn:insequens:error:email-confirmation-failed", "Email confirmation failed." },
        { "account-update", HttpStatusCode.BadRequest, "urn:insequens:error:account-update-failed", "Account update failed." },
        { "domain", HttpStatusCode.BadRequest, "urn:insequens:error:domain-rule-violated", "Domain rule violated." },
        { "validation", HttpStatusCode.BadRequest, "urn:insequens:error:validation", "Validation failed." },
        { "unhandled", HttpStatusCode.InternalServerError, "urn:insequens:error:internal", "Internal Server Error" },
    };

    [Theory]
    [MemberData(nameof(Exceptions))]
    public async Task Exception_IsReturnedAsProblemDetailsWithTypeTitleInstanceAndTraceId(
        string kind,
        HttpStatusCode status,
        string type,
        string title)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/test/throw/{kind}");

        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().Should().Be((int)status);
        json.RootElement.GetProperty("type").GetString().Should().Be(type);
        json.RootElement.GetProperty("title").GetString().Should().Be(title);
        json.RootElement.GetProperty("instance").GetString().Should().Be($"/test/throw/{kind}");
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        body.Should().NotContain("StackTrace");
    }

    [Theory]
    [InlineData("account-update", "The current password is incorrect.")]
    [InlineData("domain", "Task description must not exceed 4000 characters.")]
    public async Task Exception_WithAReasonForTheClient_ReturnsItAsDetail(string kind, string detail)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/test/throw/{kind}");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        json.RootElement.GetProperty("detail").GetString().Should().Be(detail);
    }

    [Fact]
    public async Task ValidationException_GroupsErrorsByPropertyWithoutLeakingTheExceptionMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var body = await (await client.GetAsync("/test/throw/validation")).Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("detail").GetString()!.Split("; ").Should().BeEquivalentTo(
            "Name is required.",
            "Name must be at least 3 characters.",
            "Priority must be between 0 and 3.");
        json.RootElement.GetProperty("errors").GetProperty("Name").EnumerateArray().Select(item => item.GetString()).Should().Equal(
            "Name is required.",
            "Name must be at least 3 characters.");
        json.RootElement.GetProperty("errors").GetProperty("Priority").EnumerateArray().Select(item => item.GetString())
            .Should().Equal("Priority must be between 0 and 3.");
        body.Should().NotContain("Do not leak this exception message.");
        body.Should().NotContain("FluentValidation.ValidationException");
    }

    [Fact]
    public async Task ValidationException_WithoutFailures_ReturnsAnEmptyErrorsObject()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var body = await (await client.GetAsync("/test/throw/validation-empty")).Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("detail").GetString().Should().BeEmpty();
        json.RootElement.GetProperty("errors").EnumerateObject().Should().BeEmpty();
        body.Should().NotContain("Do not leak this exception message.");
    }

    [Fact]
    public async Task ValidationException_WithAModelLevelFailure_UsesAnEmptyPropertyKey()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var body = await (await client.GetAsync("/test/throw/validation-model")).Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("errors").GetProperty("").EnumerateArray().Select(item => item.GetString())
            .Should().Equal("A general validation failure occurred.");
    }

    [Fact]
    public async Task UnhandledException_InProduction_HidesTheMessage()
    {
        await using var factory = CreateFactory(InsequensApiFactory.ProductionSettings, "Production");
        using var client = factory.CreateHttpsClient("api.insequens.test");

        var response = await client.GetAsync("/test/throw/unhandled");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred. Please try again later.");
        body.Should().NotContain("Secret failure detail.").And.NotContain("Inner secret.");
    }

    [Fact]
    public async Task UnhandledException_InDevelopment_ShowsTheMessageAndInnerMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        using var json = JsonDocument.Parse(await (await client.GetAsync("/test/throw/unhandled")).Content.ReadAsStringAsync());

        json.RootElement.GetProperty("detail").GetString()
            .Should().Be("Message: Secret failure detail. Inner Exception: Inner secret.");
    }

    [Fact]
    public async Task ErrorResponse_KeepsTheSecurityHeaders()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/test/throw/not-found");

        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
    }

    [Fact]
    public async Task UnauthorizedFromTheJwtHandler_IsProblemDetails()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/v1/ToDoItem");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        json.RootElement.GetProperty("instance").GetString().Should().Be("/v1/ToDoItem");
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnknownRoute_IsProblemDetails()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/v1/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    private static InsequensApiFactory CreateFactory(
        IReadOnlyDictionary<string, string?>? settings = null,
        string environment = "Development") =>
        new(settings, environment, services =>
            services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly));
}
