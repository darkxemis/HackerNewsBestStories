namespace HackerNewsBestStories.Application.Common.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public double ExpirationSeconds { get; set; } = 300;
}
