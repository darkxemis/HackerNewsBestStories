namespace HackerNewsBestStories.UnitTests.Features.BestStories;

using System.Net;
using FluentAssertions;
using HackerNewsBestStories.Application.Common.Interfaces;
using HackerNewsBestStories.Application.DTOs;
using HackerNewsBestStories.Application.Features.BestStories;
using HackerNewsBestStories.Core.Common.Exceptions;
using HackerNewsBestStories.Core.Common.Results;
using HackerNewsBestStories.Core.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public sealed class GetBestStoriesQueryHandlerTests
{
    private readonly Mock<IHackerNewsApiClient> _hackerNewsClient = new();
    private readonly GetBestStoriesQueryHandler _handler;

    public GetBestStoriesQueryHandlerTests()
    {
        _handler = new GetBestStoriesQueryHandler(_hackerNewsClient.Object, CreateCache());
    }

    [Fact]
    public async Task Returns_stories_ordered_by_score_highest_first()
    {
        SetupBestStoryIds(1, 2, 3);
        SetupStory(Story(1, score: 10));
        SetupStory(Story(2, score: 90));
        SetupStory(Story(3, score: 50));

        var result = await HandleAsync(new GetBestStoriesQuery(3));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(story => story.Score).Should().Equal(90, 50, 10);
    }

    [Fact]
    public async Task Fetches_only_the_requested_number_of_stories()
    {
        SetupBestStoryIds(1, 2, 3, 4);
        SetupStory(Story(1, score: 40));
        SetupStory(Story(2, score: 30));
        SetupStory(Story(3, score: 20));
        SetupStory(Story(4, score: 10));

        var result = await HandleAsync(new GetBestStoriesQuery(2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _hackerNewsClient.Verify(
            client => client.GetStoryAsync(3, It.IsAny<CancellationToken>()), Times.Never);
        _hackerNewsClient.Verify(
            client => client.GetStoryAsync(4, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Returns_fewer_stories_when_hacker_news_has_none_left()
    {
        SetupBestStoryIds(1, 2);
        SetupStory(Story(1, score: 20));
        SetupStory(Story(2, score: 10));

        var result = await HandleAsync(new GetBestStoriesQuery(5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Skips_stories_that_were_deleted_on_hacker_news()
    {
        SetupBestStoryIds(1, 2, 3);
        SetupStory(Story(1, score: 30));
        _hackerNewsClient
            .Setup(client => client.GetStoryAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Story>.Failure(Error.StoryNotFound()));
        SetupStory(Story(3, score: 20));

        var result = await HandleAsync(new GetBestStoriesQuery(3));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(story => story.Title).Should().Equal("Story 1", "Story 3");
    }

    [Fact]
    public async Task Fails_with_bad_gateway_when_the_story_ids_cannot_be_loaded()
    {
        _hackerNewsClient
            .Setup(client => client.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<int>>.Failure(Error.HackerNewsUnavailable()));

        var result = await HandleAsync(new GetBestStoriesQuery(3));

        result.IsFailure.Should().BeTrue();
        result.Error!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        result.Error.Code.Should().Be(ErrorTags.HackerNews.Unavailable);
    }

    [Fact]
    public async Task Fails_with_bad_gateway_when_a_story_cannot_be_loaded()
    {
        SetupBestStoryIds(1, 2);
        SetupStory(Story(1, score: 30));
        _hackerNewsClient
            .Setup(client => client.GetStoryAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Story>.Failure(Error.HackerNewsUnavailable()));

        var result = await HandleAsync(new GetBestStoriesQuery(2));

        result.IsFailure.Should().BeTrue();
        result.Error!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Serves_a_repeated_request_without_calling_hacker_news_again()
    {
        SetupBestStoryIds(1);
        SetupStory(Story(1, score: 42));

        var first = await HandleAsync(new GetBestStoriesQuery(1));
        var second = await HandleAsync(new GetBestStoriesQuery(1));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().BeEquivalentTo(first.Value);
        _hackerNewsClient.Verify(
            client => client.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hackerNewsClient.Verify(
            client => client.GetStoryAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Task<Result<IReadOnlyList<StoryDto>>> HandleAsync(GetBestStoriesQuery query) =>
        _handler.Handle(query, CancellationToken.None);

    private void SetupBestStoryIds(params int[] storyIds) =>
        _hackerNewsClient
            .Setup(client => client.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<int>>.Success(storyIds));

    private void SetupStory(Story story) =>
        _hackerNewsClient
            .Setup(client => client.GetStoryAsync(story.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Story>.Success(story));

    private static Story Story(int id, int score) =>
        new(id, $"Story {id}", $"https://example.com/{id}", "author", DateTimeOffset.UnixEpoch.AddSeconds(id), score, 10);

    private static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();

        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
