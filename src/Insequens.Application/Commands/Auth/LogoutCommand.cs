using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

/// <param name="SessionId">The session from the access token. Without one, every session of the user ends.</param>
public record LogoutCommand(Guid UserId, Guid? SessionId = null) : IRequest<AuthMessageResponse>;
