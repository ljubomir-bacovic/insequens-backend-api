using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record LogoutEverywhereCommand(Guid UserId) : IRequest<AuthMessageResponse>;
