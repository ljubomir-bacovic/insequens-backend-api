using Insequens.Application.Abstractions;
using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public class LogoutHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<LogoutCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await dbContext.RevokeRefreshTokensAsync(
            request.UserId,
            request.SessionId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResponses.LoggedOut;
    }
}
