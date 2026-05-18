using Microsoft.Extensions.Caching.Memory;
using HackerNews.BestStories.Api.Infrastructure;
using HackerNews.BestStories.Api.Services;
using HackerNews.BestStories.Api.Tests.Fakes;

namespace HackerNews.BestStories.Api.Tests.Services;

public sealed class BestStoriesServiceCachingTests
{
    [Fact]
    public async Task GetBestAsync_UsesCache_ToAvoidRepeatedExternalCalls()
    {
        var fake = new FakeHackerNewsClient
        {
            BestIds = [21, 22]
        };

        fake.ItemsById[21] = new() { Id = 21, Type = "story", Title = "S1", Time = 1, Score = 1, Url = "https://1" };
        fake.ItemsById[22] = new() { Id = 22, Type = "story", Title = "S2", Time = 2, Score = 2, Url = "https://2" };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var locker = new AsyncKeyedLocker();

        var svc = new BestStoriesService(fake, cache, locker);

        _ = await svc.GetBestAsync(2, CancellationToken.None);
        _ = await svc.GetBestAsync(2, CancellationToken.None);

        Assert.Equal(1, fake.GetBestIdsCalls);
        Assert.Equal(2, fake.GetItemCalls);
    }
}