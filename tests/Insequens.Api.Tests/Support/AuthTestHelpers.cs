using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Email;

namespace Insequens.Api.Tests.Support;

public static class AuthTestHelpers
{
    public const string Email = "user@example.com";
    public const string Password = "Valid-Passw0rd";

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email = Email, string password = Password) =>
        client.PostAsJsonAsync("/v1/Auth/login", new { email, password });

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string token, string refreshToken) =>
        client.PostAsJsonAsync("/v1/Auth/refresh-token", new { token, refreshToken });

    public static async Task<AuthTokensResponse> LoginForTokensAsync(this HttpClient client, string email = Email, string password = Password)
    {
        var response = await client.LoginAsync(email, password);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthTokensResponse>())!;
    }

    /// <summary>
    /// The ProblemDetails body without its per-request <c>traceId</c>, so two failures can be compared byte for byte.
    /// </summary>
    public static async Task<string> ReadProblemWithoutTraceIdAsync(this HttpResponseMessage response)
    {
        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        problem.Remove("traceId").Should().BeTrue("every ProblemDetails carries a traceId");

        return problem.ToJsonString();
    }

    public static void UseBearer(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    /// <summary>The link in an email's plain-text body, which ends with the link.</summary>
    public static Uri ExtractLink(EmailMessage message)
    {
        var text = message.TextBody ?? throw new InvalidOperationException("The email has no text body.");

        return new Uri(text[text.IndexOf("http", StringComparison.Ordinal)..]);
    }

    public static string QueryValue(Uri link, string name) => QueryHelpers.ParseQuery(link.Query)[name].ToString();
}
