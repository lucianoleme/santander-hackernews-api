using Microsoft.Extensions.Caching.Memory;
using Santander.HackerNews.Api.Infrastructure;
using Santander.HackerNews.Api.Models;
using System.Globalization;

namespace Santander.HackerNews.Api.Services;

/// <summary>
/// Application service responsible for retrieving and ranking Hacker News stories.
/// This service orchestrates data access, caching and concurrency control,
/// without performing direct HTTP calls itself.
/// </summary>
internal sealed class BestStoriesService(
    HackerNewsClient hn,
    IMemoryCache cache,
    AsyncKeyedLocker locker)
{
    private const string BestIdsCacheKey = "hn:beststories:ids";
    private const string RankedCacheKey = "hn:beststories:ranked";

    private readonly HackerNewsClient _hn = hn;
    private readonly IMemoryCache _cache = cache;
    private readonly AsyncKeyedLocker _locker = locker;

    /// <summary>
    /// Limits the number of concurrent outbound calls to the Hacker News API
    /// in order to avoid overloading the external service.
    /// </summary>
    private readonly SemaphoreSlim _hnCallLimiter = new SemaphoreSlim(16, 16);

    /// <summary>
    /// Retrieves the top N stories ordered by score in descending order.
    /// </summary>
    /// <param name="n">The maximum number of stories to return.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// A read-only list of stories sorted by score in descending order.
    /// </returns>
    public async Task<IReadOnlyList<StoryDto>> GetBestAsync(int n, CancellationToken ct)
    {
        if (n <= 0) return Array.Empty<StoryDto>();

        var ranked = await GetOrBuildRankedAsync(ct);

        // Ranked list is already sorted in descending order.
        if (ranked.Count <= n) return ranked;
        return ranked.Take(n).ToArray();
    }

    /// <summary>
    /// Retrieves the ranked list of stories from cache or rebuilds it if missing.
    /// A keyed async lock is used to prevent concurrent rebuilds.
    /// </summary>
    private async Task<IReadOnlyList<StoryDto>> GetOrBuildRankedAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue<IReadOnlyList<StoryDto>>(RankedCacheKey, out var ranked) && ranked is not null)
            return ranked;

        using (await _locker.LockAsync(RankedCacheKey, ct))
        {
            if (_cache.TryGetValue<IReadOnlyList<StoryDto>>(RankedCacheKey, out ranked) && ranked is not null)
                return ranked;

            var built = await BuildRankedAsync(ct);

            // Short TTL to reduce recomputation under burst traffic.
            _cache.Set(RankedCacheKey, built, TimeSpan.FromSeconds(60));

            return built;
        }
    }

    /// <summary>
    /// Builds the ranked list of stories by fetching all best story items,
    /// applying bounded concurrency and sorting by score.
    /// </summary>
    private async Task<IReadOnlyList<StoryDto>> BuildRankedAsync(CancellationToken ct)
    {
        var ids = await GetBestIdsCachedAsync(ct);

        // Strict approach: fetch details for all IDs with bounded concurrency.
        var tasks = ids.Select(id => GetStoryDtoSafeAsync(id, ct)).ToArray();
        var results = await Task.WhenAll(tasks);

        var stories = results
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Time)
            .ToArray();

        return stories;
    }

    /// <summary>
    /// Retrieves the list of best story IDs from cache or from the Hacker News API.
    /// </summary>
    private async Task<IReadOnlyList<long>> GetBestIdsCachedAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue<IReadOnlyList<long>>(BestIdsCacheKey, out var ids) && ids is not null)
            return ids;

        using (await _locker.LockAsync(BestIdsCacheKey, ct))
        {
            if (_cache.TryGetValue<IReadOnlyList<long>>(BestIdsCacheKey, out ids) && ids is not null)
                return ids;

            var fetched = await _hn.GetBestStoryIdsAsync(ct);

            // Short TTL for the ID list.
            _cache.Set(BestIdsCacheKey, fetched, TimeSpan.FromSeconds(60));

            return fetched;
        }
    }

    /// <summary>
    /// Retrieves a single story DTO from cache or from the Hacker News API.
    /// Only items of type "story" are considered.
    /// </summary>
    private async Task<StoryDto?> GetStoryDtoSafeAsync(long id, CancellationToken ct)
    {
        var key = $"hn:item:{id}";

        if (_cache.TryGetValue<StoryDto>(key, out var dto) && dto is not null)
            return dto;

        using (await _locker.LockAsync(key, ct))
        {
            if (_cache.TryGetValue<StoryDto>(key, out dto) && dto is not null)
                return dto;

            await _hnCallLimiter.WaitAsync(ct);
            try
            {
                var item = await _hn.GetItemAsync(id, ct);
                if (item is null) return null;

                // Only process items that represent stories.
                if (!string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase))
                    return null;

                var mapped = Map(item);

                // Longer TTL for individual story details.
                _cache.Set(key, mapped, TimeSpan.FromMinutes(10));

                return mapped;
            }
            finally
            {
                _hnCallLimiter.Release();
            }
        }
    }

    /// <summary>
    /// Maps a Hacker News item payload to the public story DTO.
    /// </summary>
    private static StoryDto Map(HackerNewsItem item)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(item.Time);
        var iso = dt.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

        var uri = item.Url;
        if (string.IsNullOrWhiteSpace(uri))
            uri = $"https://news.ycombinator.com/item?id={item.Id}";

        return new StoryDto
        {
            Title = item.Title ?? string.Empty,
            Uri = uri,
            PostedBy = item.By ?? string.Empty,
            Time = iso,
            Score = item.Score,
            CommentCount = item.Descendants
        };
    }
}