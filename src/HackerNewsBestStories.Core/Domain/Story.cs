namespace HackerNewsBestStories.Core.Domain;

public sealed record Story(
    int Id,
    string Title,
    string Uri,
    string PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount);
