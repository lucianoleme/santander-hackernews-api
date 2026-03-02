using Santander.HackerNews.Api.Infrastructure;

namespace Santander.HackerNews.Api.Tests.Fakes;

internal sealed class FakeHackerNewsClient : IHackerNewsClient
{
    public IReadOnlyList<long> BestIds { get; set; } = [];
    public Dictionary<long, HackerNewsItem> ItemsById { get; } = [];

    public int GetBestIdsCalls { get; private set; }
    public int GetItemCalls { get; private set; }

    public Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken ct)
    {
        GetBestIdsCalls++;
        return Task.FromResult(BestIds);
    }

    public Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken ct)
    {
        GetItemCalls++;
        ItemsById.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }
}