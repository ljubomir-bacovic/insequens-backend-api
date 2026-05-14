using FluentValidation;
using MediatR;

namespace Insequens.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return next(cancellationToken);
        }

        return HandleValidatedRequestAsync(request, next, cancellationToken);
    }

    private async Task<TResponse> HandleValidatedRequestAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = (await Task.WhenAll(validators.Select(validator =>
                validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken))))
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
