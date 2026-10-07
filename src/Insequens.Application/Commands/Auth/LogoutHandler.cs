using MediatR;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Commands.Auth;

public class LogoutHandler(IIdentityService identityService)
    : IRequestHandler<LogoutCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await identityService.RevokeRefreshTokenAsync(request.UserId, cancellationToken);

        return AuthResponses.LoggedOut;
    }
}
