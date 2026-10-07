using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Asp.Versioning;
using FluentAssertions;
using Insequens.Api.Controllers;
using Insequens.Api.Tests.Support;
using Insequens.Contracts.V1.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Tests.Versioning;

public class ApiVersioningTests
{
    [Fact]
    public async Task V1AndV2Tasks_ForTheSameData_ReturnTheSameBody()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));
        (await client.PostAsJsonAsync("/v1/ToDoItem", new ToDoItemCreateModel("Task", "Description", 1, null)))
            .EnsureSuccessStatusCode();

        var v1 = await client.GetStringAsync("/v1/ToDoItem");
        var v2 = await client.GetStringAsync("/v2/Tasks");

        v2.Should().Be(v1);
        JsonDocument.Parse(v1).RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task CreateOnV2_ReturnsALocationOnV2()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync("/v2/Tasks", new ToDoItemCreateModel("Task", null, 0, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ToDoItemGetDetailsModel>();
        response.Headers.Location!.AbsolutePath.Should().BeEquivalentTo($"/v2/Tasks/{created!.Id}");
        (await client.GetAsync(response.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/v1/ToDoItem", "1.0")]
    [InlineData("/v2/Tasks", "2.0")]
    public async Task Response_ReportsTheSupportedVersions(string path, string supported)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync(path);

        response.Headers.GetValues("api-supported-versions").Should().Equal(supported);
    }

    [Theory]
    [InlineData("/v2/ToDoItem")]
    [InlineData("/v3/Tasks")]
    public async Task UnsupportedVersion_IsRejectedWithProblemDetails(string path)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync(path);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("Development", "/openapi/v1.json", "/v1/ToDoItem")]
    [InlineData("Development", "/openapi/v2.json", "/v2/Tasks")]
    [InlineData("Production", "/openapi/v1.json", "/v1/ToDoItem")]
    [InlineData("Production", "/openapi/v2.json", "/v2/Tasks")]
    public async Task OpenApiDocument_ForEachVersion_IsPublicInEveryEnvironmentAndHoldsOnlyThatVersion(
        string environment,
        string document,
        string expectedPath)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateHttpsClient(environment == "Production" ? "api.insequens.test" : "localhost");

        var response = await client.GetAsync(document);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
        paths.Should().Contain(expectedPath);
        var otherVersion = expectedPath.StartsWith("/v1/", StringComparison.Ordinal) ? "/v2/" : "/v1/";
        paths.Should().NotContain(path => path.StartsWith(otherVersion, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Staging", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task ScalarUi_IsOnlyServedInDevelopmentAndStaging(string environment, HttpStatusCode expected)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateHttpsClient(environment == "Development" ? "localhost" : "api.insequens.test");
        client.UseBearer(factory.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/scalar/v1");

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("/v1/Auth/login", "post", false)]
    [InlineData("/v1/Account/confirm-email-change", "post", false)]
    [InlineData("/v1/ToDoItem", "get", true)]
    [InlineData("/v1/Auth/logout", "post", true)]
    public async Task OpenApiDocument_RequiresTheBearerSchemeOnlyWhereTheEndpointDoes(string path, string method, bool secured)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var operation = json.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);
        operation.TryGetProperty("security", out _).Should().Be(secured);
        json.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _).Should().BeTrue();
    }

    [Fact]
    public void EveryController_DeclaresAnApiVersionOrIsVersionNeutral()
    {
        // A controller without either is not routed at all once versioning is on.
        var controllers = typeof(ToDoItemController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)) && !type.IsAbstract);

        controllers.Should().AllSatisfy(controller =>
            (controller.GetCustomAttributes<ApiVersionAttribute>().Any()
                || controller.GetCustomAttribute<ApiVersionNeutralAttribute>() is not null)
            .Should().BeTrue(controller.Name));
    }

    private static InsequensApiFactory CreateFactory(string environment) =>
        environment == "Development"
            ? new InsequensApiFactory()
            : new InsequensApiFactory(InsequensApiFactory.ProductionSettings, environment);
}
