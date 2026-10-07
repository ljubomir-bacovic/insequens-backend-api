using Insequens.Application.Abstractions;
using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public class LogoutEverywhereHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<LogoutEverywhereCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(LogoutEverywhereCommand request, CancellationToken cancellationToken)
    {
        await dbContext.RevokeRefreshTokensAsync(
            request.UserId,
            familyId: null,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResponses.LoggedOutEverywhere;
    }
}
