namespace HackerNewsBestStories.Application.Common.Interfaces;

using HackerNewsBestStories.Core.Common.Results;
using HackerNewsBestStories.Core.Domain;

public interface IHackerNewsApiClient
{
    Task<Result<IReadOnlyList<int>>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    Task<Result<Story>> GetStoryAsync(int id, CancellationToken cancellationToken);
}
