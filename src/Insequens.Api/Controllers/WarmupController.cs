using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Insequens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("warmup")]
public class WarmupController : ControllerBase
{
    private readonly InsequensContext _context;

    public WarmupController(InsequensContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        return canConnect ? Ok("Healthy") : StatusCode(503, "Database unavailable");
    }
}
