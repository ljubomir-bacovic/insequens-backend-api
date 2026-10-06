using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record LogoutCommand(Guid UserId) : IRequest<AuthMessageResponse>;
