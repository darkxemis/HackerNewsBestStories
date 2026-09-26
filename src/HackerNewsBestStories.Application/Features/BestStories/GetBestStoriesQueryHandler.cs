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
                $"best-stories:{request.StoryCount}",
                async token => await FetchStoriesAsync(request.StoryCount, token),
                cancellationToken: cancellationToken);

            return Result<IReadOnlyList<StoryDto>>.Success(stories);
        }
        catch (HackerNewsRequestException ex)
        {
            return Result<IReadOnlyList<StoryDto>>.Failure(ex.Error);
        }
    }

    private async Task<List<StoryDto>> FetchStoriesAsync(int storyCount, CancellationToken cancellationToken)
    {
        var bestStoryIdsResult = await hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);

        if (bestStoryIdsResult.IsFailure)
        {
            throw new HackerNewsRequestException(bestStoryIdsResult.Error!);
        }

        var topStoryIds = bestStoryIdsResult.Value!.Take(storyCount);

        var fetchedStories = await Task.WhenAll(
            topStoryIds.Select(id => hackerNewsClient.GetStoryAsync(id, cancellationToken)));

        var stories = new List<Story>(fetchedStories.Length);

        foreach (var fetchedStory in fetchedStories)
        {
            if (fetchedStory.IsFailure)
            {
                if (fetchedStory.Error!.Code == ErrorTags.HackerNews.StoryNotFound)
                {
                    continue;
                }

                throw new HackerNewsRequestException(fetchedStory.Error);
            }

            stories.Add(fetchedStory.Value!);
        }

        return stories
            .OrderByDescending(story => story.Score)
            .ThenByDescending(story => story.Id)
            .Take(storyCount)
            .Select(StoryDto.From)
            .ToList();
    }

    private sealed class HackerNewsRequestException(Error error) : Exception(error.Message)
    {
        public Error Error { get; } = error;
    }
}
