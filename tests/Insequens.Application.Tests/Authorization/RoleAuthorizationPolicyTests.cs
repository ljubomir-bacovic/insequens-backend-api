using FluentAssertions;
using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Authorization;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Insequens.Application.Tests.Authorization;

public class RoleAuthorizationPolicyTests
{
    private readonly TestCurrentUser _currentUser = new() { UserId = Guid.NewGuid() };
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    [Fact]
    public async Task AuthorizeAsync_WithoutRequiredRole_PassesWithoutAskingTheUserStore()
    {
        var policy = CreatePolicy<Unrestricted>();

        await policy.AuthorizeAsync(new Unrestricted(), CancellationToken.None);

        await _identityService.DidNotReceiveWithAnyArgs().IsInRoleAsync(default, default!, default);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasTheRole_Passes()
    {
        InRole(Roles.Admin);
        var policy = CreatePolicy<AdminOnly>();

        var action = () => policy.AuthorizeAsync(new AdminOnly(), CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserLacksTheRole_ThrowsForbidden()
    {
        var policy = CreatePolicy<AdminOnly>();

        var action = () => policy.AuthorizeAsync(new AdminOnly(), CancellationToken.None);

        (await action.Should().ThrowAsync<ForbiddenException>()).Which.RequiredRole.Should().Be(Roles.Admin);
    }

    [Fact]
    public async Task AuthorizeAsync_WithoutAuthenticatedUser_ThrowsForbidden()
    {
        _currentUser.UserId = null;
        var policy = CreatePolicy<AdminOnly>();

        var action = () => policy.AuthorizeAsync(new AdminOnly(), CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AuthorizeAsync_WithSeveralRequiredRoles_RequiresEveryOne()
    {
        InRole(Roles.Admin);
        var policy = CreatePolicy<AdminAndSupport>();

        var action = () => policy.AuthorizeAsync(new AdminAndSupport(), CancellationToken.None);

        (await action.Should().ThrowAsync<ForbiddenException>()).Which.RequiredRole.Should().Be(Roles.Support);
    }

    private RoleAuthorizationPolicy<TRequest> CreatePolicy<TRequest>()
        where TRequest : notnull
    {
        var services = new ServiceCollection()
            .AddSingleton<ICurrentUser>(_currentUser)
            .AddSingleton(_identityService)
            .BuildServiceProvider();

        return new RoleAuthorizationPolicy<TRequest>(services);
    }

    private void InRole(string role) =>
        _identityService.IsInRoleAsync(_currentUser.UserId!.Value, role, Arg.Any<CancellationToken>()).Returns(true);

    public sealed record Unrestricted;

    [RequiresRole(Roles.Admin)]
    public sealed record AdminOnly;

    [RequiresRole(Roles.Admin)]
    [RequiresRole(Roles.Support)]
    public sealed record AdminAndSupport;
}
