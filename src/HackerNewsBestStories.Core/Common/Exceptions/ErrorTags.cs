namespace HackerNewsBestStories.Core.Common.Exceptions;

public static class ErrorTags
{
    public static class Server
    {
        public const string InternalError = "server.internalError";
    }

    public static class HackerNews
    {
        public const string Unavailable = "hackerNews.unavailable";
        public const string StoryNotFound = "hackerNews.storyNotFound";
    }

    public static class Validation
    {
        public const string Failed = "validation.failed";
    }
}
