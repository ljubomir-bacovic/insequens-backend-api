using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Tests.Security;

/// <summary>A controller with no authorization attributes, to prove the fallback policy protects it.</summary>
[ApiController]
[ApiVersionNeutral]
[Route("test/unprotected")]
public class UnprotectedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("reached");
}
