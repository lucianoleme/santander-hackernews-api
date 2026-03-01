namespace Santander.HackerNews.Api.Infrastructure;

/// <summary>
/// Thin HTTP client responsible for retrieving data from the official Hacker News API.
/// This type contains no business rules, caching or ranking logic.
/// </summary>
internal sealed class HackerNewsClient(HttpClient http): IHackerNewsClient
{
    /// <summary>
    /// Retrieves the list of item IDs representing the current "best stories" set.
    /// </summary>
    public async Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken ct)
    {
        var ids = await http.GetFromJsonAsync<long[]>("v0/beststories.json", ct);
        return ids ?? [];
    }

    /// <summary>
    /// Retrieves a single Hacker News item by ID.
    /// The item may represent a story, comment, job, poll or other item types.
    /// </summary>
    public async Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken ct)
    {
        return await http.GetFromJsonAsync<HackerNewsItem>($"v0/item/{id}.json", ct);
    }
}

/// <summary>
/// Represents the JSON payload returned by the Hacker News API for an item.
/// Items can be stories, comments, jobs, polls and related types.
/// </summary>
internal sealed class HackerNewsItem
{
    /// <summary>
    /// Unique identifier of the item.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Title of the item when applicable, for example for stories and jobs.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// External URL associated with the item when applicable.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Username of the author who submitted the item when available.
    /// </summary>
    public string? By { get; set; }

    /// <summary>
    /// Creation time expressed as Unix time in seconds.
    /// </summary>
    public long Time { get; set; }

    /// <summary>
    /// Score of the item when applicable, typically for stories.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Total comment count when applicable, typically present on stories.
    /// </summary>
    public int Descendants { get; set; }

    /// <summary>
    /// Item type, for example "story".
    /// </summary>
    public string? Type { get; set; }
}
