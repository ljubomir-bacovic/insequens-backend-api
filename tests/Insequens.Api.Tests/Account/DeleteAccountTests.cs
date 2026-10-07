using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Application.Commands.Account;
using Insequens.Contracts.V1.Account;
using Insequens.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Account;

/// <summary>On SQLite, so foreign keys and cascades behave as on SQL Server and every table can be scanned.</summary>
public sealed class DeleteAccountTests : IAsyncDisposable
{
    private readonly InsequensApiFactory _factory = new(relationalDatabase: true);

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Delete_WithCorrectPassword_Returns202AndSignInStopsAtOnce()
    {
        await _factory.CreateUserAsync(Email, Password);
        using var client = _factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        client.UseBearer(tokens.Token);

        var response = await client.PostAsJsonAsync("/v1/Account/deletion", new { currentPassword = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadFromJsonAsync<AccountDeletionResponse>())!.DeletesAfter
            .Should().Be(_factory.Clock.GetUtcNow().AddDays(30));
        (await client.LoginAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.RefreshAsync(tokens.Token, tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email })).StatusCode.Should().Be(HttpStatusCode.Accepted);
        _factory.EmailSender.Messages.Should().BeEmpty("a deleted account gets no password reset");
    }

    [Fact]
    public async Task Purge_AfterTheGracePeriod_LeavesNoRowWithTheEmailInAnyTable()
    {
        await _factory.CreateUserAsync(Email, Password);
        await _factory.CreateUserAsync("other@example.com", Password);
        using var client = _factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);
        (await client.PostAsJsonAsync("/v1/ToDoItem", new { name = "Task", priority = 1 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/v1/Account/deletion", new { currentPassword = Password })).EnsureSuccessStatusCode();
        _factory.Clock.Advance(TimeSpan.FromDays(30));

        var purged = await PurgeAsync();

        purged.Should().Be(1);
        (await RowsContainingAsync(Email)).Should().BeEmpty();
        (await CountAsync("Tasks")).Should().Be(0);
        (await RowsContainingAsync("other@example.com")).Should().NotBeEmpty("other accounts are untouched");
    }

    [Fact]
    public async Task Purge_DuringTheGracePeriod_KeepsTheAccount()
    {
        await _factory.CreateUserAsync(Email, Password);
        using var client = _factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);
        (await client.PostAsJsonAsync("/v1/Account/deletion", new { currentPassword = Password })).EnsureSuccessStatusCode();
        _factory.Clock.Advance(TimeSpan.FromDays(29));

        var purged = await PurgeAsync();

        purged.Should().Be(0);
        (await RowsContainingAsync(Email)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Delete_WithWrongPassword_Returns400AndKeepsTheAccount()
    {
        await _factory.CreateUserAsync(Email, Password);
        using var client = _factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.PostAsJsonAsync("/v1/Account/deletion", new { currentPassword = "Wrong-Passw0rd" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.LoginAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        using var client = _factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Account/deletion", new { currentPassword = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<int> PurgeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new PurgeDeletedAccountsCommand());
    }

    private async Task<int> CountAsync(string table)
    {
        using var scope = _factory.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<InsequensContext>().Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\"";

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    /// <summary>"Table: value" for every column value in every table that contains <paramref name="text"/>.</summary>
    private async Task<List<string>> RowsContainingAsync(string text)
    {
        using var scope = _factory.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<InsequensContext>().Database.GetDbConnection();
        var tables = new List<string>();
        await using (var listTables = connection.CreateCommand())
        {
            listTables.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
            await using var reader = await listTables.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        tables.Should().Contain(["AspNetUsers", "Tasks", "RefreshToken"]);
        var matches = new List<string>();
        foreach (var table in tables)
        {
            await using var select = connection.CreateCommand();
            select.CommandText = $"SELECT * FROM \"{table}\"";
            await using DbDataReader reader = await select.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    var value = reader.IsDBNull(column) ? null : Convert.ToString(reader.GetValue(column));
                    if (value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        matches.Add($"{table}: {value}");
                    }
                }
            }
        }

        return matches;
    }
}
