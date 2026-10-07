using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record LogoutCommand(Guid UserId) : IRequest<AuthMessageResponse>;
