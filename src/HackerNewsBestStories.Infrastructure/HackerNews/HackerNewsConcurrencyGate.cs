namespace HackerNewsBestStories.Infrastructure.HackerNews;

public sealed class HackerNewsConcurrencyGate(int maxConcurrentRequests) : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(maxConcurrentRequests, maxConcurrentRequests);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            return await operation(cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Dispose() => semaphore.Dispose();
}
