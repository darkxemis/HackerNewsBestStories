namespace HackerNewsBestStories.Application.Features.BestStories;

using HackerNewsBestStories.Application.Common.Interfaces;
using HackerNewsBestStories.Application.DTOs;
using HackerNewsBestStories.Core.Common.Exceptions;
using HackerNewsBestStories.Core.Common.Results;
using HackerNewsBestStories.Core.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

public sealed class GetBestStoriesQueryHandler(
    IHackerNewsApiClient hackerNewsClient,
    HybridCache cache) : IRequestHandler<GetBestStoriesQuery, Result<IReadOnlyList<StoryDto>>>
{
    public async Task<Result<IReadOnlyList<StoryDto>>> Handle(
        GetBestStoriesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stories = await cache.GetOrCreateAsync(
                $"best-stories:{request.N}",
                async token => await FetchStoriesAsync(request.N, token),
                cancellationToken: cancellationToken);

            return Result<IReadOnlyList<StoryDto>>.Success(stories);
        }
        catch (HackerNewsRequestException ex)
        {
            return Result<IReadOnlyList<StoryDto>>.Failure(ex.Error);
        }
    }

    private async Task<List<StoryDto>> FetchStoriesAsync(int n, CancellationToken cancellationToken)
    {
        var idsResult = await hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);

        if (idsResult.IsFailure)
        {
            throw new HackerNewsRequestException(idsResult.Error!);
        }

        var ids = idsResult.Value!.Take(n);

        var storyResults = await Task.WhenAll(
            ids.Select(id => hackerNewsClient.GetStoryAsync(id, cancellationToken)));

        var stories = new List<Story>(storyResults.Length);

        foreach (var storyResult in storyResults)
        {
            if (storyResult.IsFailure)
            {
                if (storyResult.Error!.Code == ErrorTags.HackerNews.StoryNotFound)
                {
                    continue;
                }

                throw new HackerNewsRequestException(storyResult.Error);
            }

            stories.Add(storyResult.Value!);
        }

        return stories
            .OrderByDescending(story => story.Score)
            .ThenByDescending(story => story.Id)
            .Take(n)
            .Select(StoryDto.From)
            .ToList();
    }

    private sealed class HackerNewsRequestException(Error error) : Exception(error.Message)
    {
        public Error Error { get; } = error;
    }
}
