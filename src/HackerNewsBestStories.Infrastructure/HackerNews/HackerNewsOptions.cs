namespace HackerNewsBestStories.Infrastructure.HackerNews;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    public string BaseAddress { get; set; } = "https://hacker-news.firebaseio.com/v0/";

    public double RequestTimeoutSeconds { get; set; } = 10;

    public double TotalTimeoutSeconds { get; set; } = 30;

    public int MaxConcurrentRequests { get; set; } = 8;
}
