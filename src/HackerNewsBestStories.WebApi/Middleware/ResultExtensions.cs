namespace HackerNewsBestStories.WebApi.Middleware;

using HackerNewsBestStories.Core.Common.Results;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        HttpContext httpContext,
        Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var problem = ApiProblem.FromError(httpContext, result.Error!);

        return Results.Json(problem, statusCode: problem.Status);
    }
}
