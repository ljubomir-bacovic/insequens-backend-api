using Insequens.Application.Exceptions;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Insequens.Application.Commands.Auth;

public class LoginHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    ILogger<LoginHandler> logger)
    : IRequestHandler<LoginCommand, AuthTokensResponse>
{
    public async Task<AuthTokensResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Login failed: no account for the email address");
            throw new AuthenticationFailedException();
        }

        var status = await identityService.CheckPasswordSignInAsync(user.Id, request.Password, cancellationToken);
        if (status != PasswordSignInStatus.Succeeded)
        {
            logger.LogInformation("Login failed for user {UserId}: {SignInStatus}", user.Id, status);
            throw new AuthenticationFailedException();
        }

        return await AuthTokenIssuer.IssueAsync(tokenService, identityService, user, cancellationToken);
    }
}
