namespace HackerNewsBestStories.WebApi.Middleware;

using System.Diagnostics;
using System.Net;
using HackerNewsBestStories.Core.Common.Results;
using Microsoft.AspNetCore.Mvc;

public static class ApiProblem
{
    public static ProblemDetails FromError(HttpContext httpContext, Error error) =>
        Create(httpContext, error.StatusCode, error.Code, error.Message, error.Metadata);

    public static ProblemDetails Create(
        HttpContext httpContext,
        HttpStatusCode statusCode,
        string tag,
        string message,
        Dictionary<string, string>? errors = null,
        string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Type = "about:blank",
            Title = message,
            Detail = detail,
        };

        problem.Extensions["tag"] = tag;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (errors is { Count: > 0 })
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }
}
