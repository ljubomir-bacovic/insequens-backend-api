using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Contracts.V1.Account;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Account;

public class ExportUserDataTests
{
    [Fact]
    public async Task Export_ReturnsTheUsersOwnDataAsAJsonAttachment()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        await factory.CreateUserAsync("other@example.com", Password);
        using var other = factory.CreateHttpsClient();
        other.UseBearer((await other.LoginForTokensAsync("other@example.com")).Token);
        (await other.PostAsJsonAsync("/v1/ToDoItem", new { name = "Not mine", priority = 1 })).EnsureSuccessStatusCode();
        using var client = factory.CreateHttpsClient();
        (await client.LoginAsync()).EnsureSuccessStatusCode();
        client.UseBearer((await client.LoginForTokensAsync()).Token);
        (await client.PostAsJsonAsync("/v1/ToDoItem", new { name = "Mine", priority = 2 })).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/v1/Account/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition.FileName.Should().Be("insequens-data-export.json");
        var export = await response.Content.ReadFromJsonAsync<UserDataExport>();
        export!.Account.Id.Should().Be(user.Id);
        export.Account.Email.Should().Be(Email);
        export.Tasks.Should().ContainSingle().Which.Name.Should().Be("Mine");
        export.Sessions.Should().HaveCount(2, "the user signed in twice");
    }

    [Fact]
    public async Task Export_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/v1/Account/export");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
