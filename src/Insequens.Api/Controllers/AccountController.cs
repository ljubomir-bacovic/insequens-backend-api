using System.Security.Claims;
using Insequens.Api.RateLimiting;
using Insequens.Application.Commands.Account;
using Insequens.Application.Queries.Account;
using Insequens.Contracts.V1.Account;
using Insequens.Contracts.V1.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Insequens.Api.Controllers;

/// <summary>The signed-in user's own account. Actions that need the current password use the stricter auth limit.</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route(Constants.BaseUrl)]
[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController : ControllerBase
{
    private const string ExportFileName = "insequens-data-export.json";

    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [HttpPost("change-email")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _mediator.Send(
            new RequestEmailChangeCommand(userId, request.NewEmail, request.CurrentPassword),
            cancellationToken);
        return Accepted(response);
    }

    /// <summary>Anonymous: the link may be opened on a device where the user is not signed in.</summary>
    [AllowAnonymous]
    [HttpPost("confirm-email-change")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new ConfirmEmailChangeCommand(request.UserId, request.NewEmail, request.Token),
            cancellationToken);
        return Ok(response);
    }

    [HttpPost("deletion")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AccountDeletionResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _mediator.Send(new DeleteAccountCommand(userId, request.CurrentPassword), cancellationToken);
        return Accepted(response);
    }

    [HttpGet("export")]
    [ProducesResponseType<UserDataExport>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportUserData(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var export = await _mediator.Send(new ExportUserDataQuery(userId), cancellationToken);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{ExportFileName}\"";
        return Ok(export);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}
