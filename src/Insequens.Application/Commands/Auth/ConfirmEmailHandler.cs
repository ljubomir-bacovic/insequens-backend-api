using Insequens.Application.Exceptions;
using MediatR;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Commands.Auth;

public class ConfirmEmailHandler(IIdentityService identityService)
    : IRequestHandler<ConfirmEmailCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var userId)
            || !await identityService.ConfirmEmailAsync(userId, request.Token, cancellationToken))
        {
            throw new EmailConfirmationFailedException();
        }

        return AuthResponses.EmailConfirmed;
    }
}
