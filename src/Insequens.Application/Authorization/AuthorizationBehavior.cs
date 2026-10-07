using MediatR;

namespace Insequens.Application.Authorization;

/// <summary>
/// Runs every <see cref="IAuthorizationPolicy{TRequest}"/> before the handler: roles first, then resource ownership.
/// The first policy that denies the request stops it.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(IEnumerable<IAuthorizationPolicy<TRequest>> policies)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (var policy in policies)
        {
            await policy.AuthorizeAsync(request, cancellationToken);
        }

        return await next(cancellationToken);
    }
}
