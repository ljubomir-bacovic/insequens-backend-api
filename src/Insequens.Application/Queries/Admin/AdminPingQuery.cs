using Insequens.Application.Authorization;
using Insequens.Contracts.V1.Admin;
using MediatR;

namespace Insequens.Application.Queries.Admin;

/// <summary>Confirms the caller reached an admin-only request; the smallest use of a role policy.</summary>
[RequiresRole(Roles.Admin)]
public record AdminPingQuery : IRequest<AdminPingResponse>;
