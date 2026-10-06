using Insequens.Application.Exceptions;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using MediatR;

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
