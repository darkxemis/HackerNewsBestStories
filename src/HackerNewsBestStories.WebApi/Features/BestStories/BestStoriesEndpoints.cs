namespace HackerNewsBestStories.WebApi.Features.BestStories;

using HackerNewsBestStories.Application.Features.BestStories;
using HackerNewsBestStories.WebApi.Middleware;
using MediatR;

public static class BestStoriesEndpoints
{
    private const int DefaultStoryCount = 10;

    public static IEndpointRouteBuilder MapBestStoriesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1")
            .MapGet("/top-stories", GetTopStoriesAsync)
            .WithName("GetTopStories")
            .WithSummary("Returns the best Hacker News stories, highest score first.")
            .WithDescription(
                "storyCount is how many stories you want back — the n from the exercise brief, " +
                "renamed so it reads better in the query string. Optional, defaults to 10, max 500.")
            .WithTags("Best Stories");

        return endpoints;
    }

    private static async Task<IResult> GetTopStoriesAsync(
        int? storyCount,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBestStoriesQuery(storyCount ?? DefaultStoryCount),
            cancellationToken);

        return result.ToHttpResult(httpContext, stories => Results.Ok(stories));
    }
}
