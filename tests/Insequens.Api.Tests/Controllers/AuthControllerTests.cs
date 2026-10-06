using System.Reflection;
using FluentAssertions;
using Insequens.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Tests.Controllers;

public class AuthControllerTests
{
    public static TheoryData<string> AnonymousActions =>
        [
            nameof(AuthController.Register),
            nameof(AuthController.ConfirmEmail),
            nameof(AuthController.Login),
            nameof(AuthController.RefreshToken),
            nameof(AuthController.ForgotPassword),
            nameof(AuthController.ResetPassword),
        ];

    [Fact]
    public void Constructor_WhenInspected_DependsOnlyOnMediator()
    {
        var constructor = typeof(AuthController).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Should().ContainSingle()
            .Which.ParameterType.Should().Be(typeof(IMediator));
    }

    [Theory]
    [MemberData(nameof(AnonymousActions))]
    public void Action_ForAnonymousFlow_IsExplicitlyAllowAnonymous(string methodName)
    {
        var method = typeof(AuthController).GetMethod(methodName)!;

        method.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void Controller_WhenInspected_HasNoClassLevelAllowAnonymous()
    {
        // A class-level [AllowAnonymous] would also lift [Authorize] from logout.
        typeof(AuthController).GetCustomAttribute<AllowAnonymousAttribute>().Should().BeNull();
        typeof(AuthController).GetMethod(nameof(AuthController.LogOut))!
            .GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void Actions_WhenInspected_DeclareResponseTypes()
    {
        var actions = typeof(AuthController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        actions.Should().NotBeEmpty();
        actions.Should().AllSatisfy(action =>
            action.GetCustomAttributes<ProducesResponseTypeAttribute>().Should().NotBeEmpty(action.Name));
    }
}
