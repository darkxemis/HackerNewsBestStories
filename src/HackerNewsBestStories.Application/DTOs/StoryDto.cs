namespace HackerNewsBestStories.Application.DTOs;

using HackerNewsBestStories.Core.Domain;

public sealed record StoryDto(
    string Title,
    string Uri,
    string PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount)
{
    public static StoryDto From(Story story) =>
        new(story.Title, story.Uri, story.PostedBy, story.Time, story.Score, story.CommentCount);
}
