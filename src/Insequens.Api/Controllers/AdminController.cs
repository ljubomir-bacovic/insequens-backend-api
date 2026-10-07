using Insequens.Api.Security;
using Insequens.Application.Queries.Admin;
using Insequens.Contracts.V1.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route(Constants.BaseUrl)]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ping")]
    [ProducesResponseType<AdminPingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Ping(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new AdminPingQuery(), cancellationToken);
        return Ok(response);
    }
}
