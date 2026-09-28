namespace HackerNewsBestStories.Application.Common.Behaviors;

using FluentValidation;
using HackerNewsBestStories.Core.Common.Results;
using MediatR;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = failures
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.First().ErrorMessage);

        if (errors.Count > 0)
        {
            var error = Error.Validation(errors);

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                return (TResponse)Result.CreateFailureMethod
                    .MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0])
                    .Invoke(null, [error])!;
            }

            throw new ValidationException(failures.SelectMany(f => f.Errors));
        }

        return await next(cancellationToken);
    }
}
