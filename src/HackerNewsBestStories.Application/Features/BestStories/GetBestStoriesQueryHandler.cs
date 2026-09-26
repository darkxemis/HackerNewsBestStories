namespace HackerNewsBestStories.Application.Features.BestStories;

using HackerNewsBestStories.Application.Common.Interfaces;
using HackerNewsBestStories.Application.DTOs;
using HackerNewsBestStories.Core.Common.Exceptions;
using HackerNewsBestStories.Core.Common.Results;
using HackerNewsBestStories.Core.Domain;
using MediatR;

public sealed class GetBestStoriesQueryHandler(
    IHackerNewsApiClient hackerNewsClient) : IRequestHandler<GetBestStoriesQuery, Result<IReadOnlyList<StoryDto>>>
{
    public async Task<Result<IReadOnlyList<StoryDto>>> Handle(
        GetBestStoriesQuery request,
        CancellationToken cancellationToken)
    {
        var idsResult = await hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);

        if (idsResult.IsFailure)
        {
            return Result<IReadOnlyList<StoryDto>>.Failure(idsResult.Error!);
        }

        var ids = idsResult.Value!.Take(request.N);

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

                return Result<IReadOnlyList<StoryDto>>.Failure(storyResult.Error);
            }

            stories.Add(storyResult.Value!);
        }

        var storyDtos = stories
            .OrderByDescending(story => story.Score)
            .ThenByDescending(story => story.Id)
            .Take(request.N)
            .Select(StoryDto.From)
            .ToList();

        return Result<IReadOnlyList<StoryDto>>.Success(storyDtos);
    }
}
