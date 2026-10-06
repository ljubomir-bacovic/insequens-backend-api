using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using MediatR;

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
