namespace Santander.HackerNews.Api.Infrastructure;

/// <summary>
/// Abstraction for a client that retrieves data from the Hacker News API.
/// This interface exists to enable testability and separation of concerns.
/// </summary>
internal interface IHackerNewsClient
{
    /// <summary>
    /// Retrieves the list of item IDs representing the current "best stories" set.
    /// </summary>
    Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves a single Hacker News item by its identifier.
    /// </summary>
    Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken ct);
}