using Microsoft.Extensions.Caching.Memory;
using Santander.HackerNews.Api.Infrastructure;
using Santander.HackerNews.Api.Services;
using Santander.HackerNews.Api.Tests.Fakes;

namespace Santander.HackerNews.Api.Tests.Services;

public sealed class BestStoriesServiceRankingTests
{
    [Fact]
    public async Task GetBestAsync_SortsByScoreDescending_AndReturnsTopN()
    {
        var fake = new FakeHackerNewsClient
        {
            BestIds = [1, 2, 3]
        };

        fake.ItemsById[1] = new() { Id = 1, Type = "story", Title = "A", By = "u1", Time = 1, Score = 10, Descendants = 0, Url = "https://a" };
        fake.ItemsById[2] = new() { Id = 2, Type = "story", Title = "B", By = "u2", Time = 2, Score = 50, Descendants = 0, Url = "https://b" };
        fake.ItemsById[3] = new() { Id = 3, Type = "story", Title = "C", By = "u3", Time = 3, Score = 20, Descendants = 0, Url = "https://c" };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var locker = new AsyncKeyedLocker();

        var svc = new BestStoriesService(fake, cache, locker);

        var result = await svc.GetBestAsync(2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("B", result[0].Title);
        Assert.Equal("C", result[1].Title);
    }

    [Fact]
    public async Task GetBestAsync_FiltersOutNonStoryItems()
    {
        var fake = new FakeHackerNewsClient
        {
            BestIds = [10, 11]
        };

        fake.ItemsById[10] = new() { Id = 10, Type = "comment", Title = "Ignore", Time = 1, Score = 999 };
        fake.ItemsById[11] = new() { Id = 11, Type = "story", Title = "Valid", Time = 2, Score = 1, Url = "https://x" };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var locker = new AsyncKeyedLocker();

        var svc = new BestStoriesService(fake, cache, locker);

        var result = await svc.GetBestAsync(10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Valid", result[0].Title);
    }

    [Fact]
    public async Task GetBestAsync_WhenNIsZero_ReturnsEmpty()
    {
        var fake = new FakeHackerNewsClient();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var locker = new AsyncKeyedLocker();

        var svc = new BestStoriesService(fake, cache, locker);

        var result = await svc.GetBestAsync(0, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBestAsync_WhenItemUrlIsMissing_UsesHackerNewsItemLink()
    {
        // Arrange
        var fake = new FakeHackerNewsClient
        {
            BestIds = [42]
        };

        fake.ItemsById[42] = new HackerNewsItem
        {
            Id = 42,
            Type = "story",
            Title = "Ask HN",
            By = "user",
            Time = 1,
            Score = 10,
            Descendants = 0,
            Url = null
        };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var locker = new AsyncKeyedLocker();
        var svc = new BestStoriesService(fake, cache, locker);

        // Act
        var result = await svc.GetBestAsync(1, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(
            "https://news.ycombinator.com/item?id=42",
            result[0].Uri
        );
    }
}