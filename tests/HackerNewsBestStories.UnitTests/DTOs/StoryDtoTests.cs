namespace HackerNewsBestStories.UnitTests.DTOs;

using System.Text.Json;
using FluentAssertions;
using HackerNewsBestStories.Application.DTOs;
using HackerNewsBestStories.Core.Domain;

public sealed class StoryDtoTests
{
    [Fact]
    public void From_maps_every_story_field()
    {
        var time = new DateTimeOffset(2019, 10, 12, 13, 43, 1, TimeSpan.Zero);
        var story = new Story(
            21233041,
            "A uBlock Origin update was rejected from the Chrome Web Store",
            "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
            "ismaildonmez",
            time,
            1716,
            572);

        var dto = StoryDto.From(story);

        dto.Should().Be(new StoryDto(
            "A uBlock Origin update was rejected from the Chrome Web Store",
            "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
            "ismaildonmez",
            time,
            1716,
            572));
    }

    [Fact]
    public void Serializes_with_the_field_names_from_the_exercise()
    {
        var dto = new StoryDto(
            "A uBlock Origin update was rejected from the Chrome Web Store",
            "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
            "ismaildonmez",
            new DateTimeOffset(2019, 10, 12, 13, 43, 1, TimeSpan.Zero),
            1716,
            572);

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("title").GetString().Should()
            .Be("A uBlock Origin update was rejected from the Chrome Web Store");
        root.GetProperty("uri").GetString().Should()
            .Be("https://github.com/uBlockOrigin/uBlock-issues/issues/745");
        root.GetProperty("postedBy").GetString().Should().Be("ismaildonmez");
        root.GetProperty("time").GetString().Should().Be("2019-10-12T13:43:01+00:00");
        root.GetProperty("score").GetInt32().Should().Be(1716);
        root.GetProperty("commentCount").GetInt32().Should().Be(572);
    }
}
