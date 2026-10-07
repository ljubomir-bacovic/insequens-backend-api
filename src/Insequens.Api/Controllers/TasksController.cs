using Asp.Versioning;
using Insequens.Api.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Controllers;

/// <summary>The task endpoints at <c>/v2/Tasks</c>.</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiVersion(ApiVersions.V2)]
[Route(Constants.BaseUrl)]
[ApiController]
public class TasksController(IMediator mediator) : TasksControllerBase(mediator);
