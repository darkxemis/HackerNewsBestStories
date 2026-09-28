namespace HackerNewsBestStories.Core.Common.Results;

using System.Net;
using HackerNewsBestStories.Core.Common.Exceptions;

public sealed record Error(
    HttpStatusCode StatusCode,
    string Code,
    string Message,
    Dictionary<string, string>? Metadata = null)
{
    public static Error Validation(Dictionary<string, string> errors) =>
        new(HttpStatusCode.BadRequest, ErrorTags.Validation.Failed, "One or more validation errors occurred.", errors);

    public static Error HackerNewsUnavailable() =>
        new(HttpStatusCode.BadGateway, ErrorTags.HackerNews.Unavailable, "The Hacker News API is currently unavailable.");

    public static Error StoryNotFound() =>
        new(HttpStatusCode.NotFound, ErrorTags.HackerNews.StoryNotFound, "The requested story was not found.");

    public static Error InternalError() =>
        new(HttpStatusCode.InternalServerError, ErrorTags.Server.InternalError, "An unexpected error occurred.");
}
