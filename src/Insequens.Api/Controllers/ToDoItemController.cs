using Asp.Versioning;
using Insequens.Api.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Controllers;

/// <summary>The task endpoints at <c>/v1/ToDoItem</c>. Frozen: new behaviour goes to <see cref="TasksController"/>.</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiVersion(ApiVersions.V1)]
[Route(Constants.BaseUrl)]
[ApiController]
public class ToDoItemController(IMediator mediator) : TasksControllerBase(mediator);
