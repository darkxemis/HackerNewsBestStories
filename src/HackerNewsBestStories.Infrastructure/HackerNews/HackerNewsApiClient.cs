namespace HackerNewsBestStories.Infrastructure.HackerNews;

using System.Net.Http.Json;
using System.Text.Json;
using HackerNewsBestStories.Application.Common.Interfaces;
using HackerNewsBestStories.Core.Common.Results;
using HackerNewsBestStories.Core.Domain;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

public sealed class HackerNewsApiClient(
    IHttpClientFactory httpClientFactory,
    HackerNewsConcurrencyGate concurrencyGate,
    ILogger<HackerNewsApiClient> logger) : IHackerNewsApiClient
{
    public const string ApiClientName = "HackerNews";

    public async Task<Result<IReadOnlyList<int>>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            client => client.GetFromJsonAsync<int[]>("beststories.json", cancellationToken),
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<IReadOnlyList<int>>.Failure(result.Error!);
        }

        if (result.Value is null)
        {
            return Result<IReadOnlyList<int>>.Failure(Error.HackerNewsUnavailable());
        }

        return Result<IReadOnlyList<int>>.Success(result.Value);
    }

    public async Task<Result<Story>> GetStoryAsync(int id, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            client => client.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken),
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<Story>.Failure(result.Error!);
        }

        if (result.Value is null)
        {
            return Result<Story>.Failure(Error.StoryNotFound());
        }

        return Result<Story>.Success(MapToStory(result.Value));
    }

    private async Task<Result<T>> ExecuteAsync<T>(
        Func<HttpClient, Task<T>> sendRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(ApiClientName);

            var value = await concurrencyGate.ExecuteAsync(_ => sendRequest(client), cancellationToken);

            return Result<T>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is HttpRequestException
            or JsonException
            or OperationCanceledException
            or BrokenCircuitException
            or TimeoutRejectedException
            or RateLimiterRejectedException)
        {
            logger.LogError(ex, "Hacker News API request failed");

            return Result<T>.Failure(Error.HackerNewsUnavailable());
        }
    }

    private static Story MapToStory(HackerNewsItem item) =>
        new(
            item.Id,
            item.Title ?? string.Empty,
            item.Url ?? $"https://news.ycombinator.com/item?id={item.Id}",
            item.By ?? string.Empty,
            DateTimeOffset.FromUnixTimeSeconds(item.Time),
            item.Score,
            item.Descendants ?? 0);
}
