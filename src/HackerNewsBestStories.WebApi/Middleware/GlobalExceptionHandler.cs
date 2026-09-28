namespace HackerNewsBestStories.WebApi.Middleware;

using System.Net;
using FluentValidation;
using HackerNewsBestStories.Core.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, tag, message, errors) = exception switch
        {
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                ErrorTags.Validation.Failed,
                "One or more validation errors occurred.",
                validation.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.First().ErrorMessage)),
            _ => (
                HttpStatusCode.InternalServerError,
                ErrorTags.Server.InternalError,
                "An unexpected error occurred.",
                (Dictionary<string, string>?)null),
        };

        logger.LogError(exception, "Unhandled exception — Tag: {Tag}", tag);

        var detail = environment.IsDevelopment()
            ? $"{exception.GetType().Name}: {exception.Message}"
            : null;

        var problem = ApiProblem.Create(httpContext, statusCode, tag, message, errors, detail);

        httpContext.Response.StatusCode = (int)statusCode;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
