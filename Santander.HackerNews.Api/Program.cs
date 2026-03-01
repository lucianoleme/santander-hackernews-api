using Santander.HackerNews.Api.Infrastructure;
using Santander.HackerNews.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<AsyncKeyedLocker>();

builder.Services.AddHttpClient<HackerNewsClient>(http =>
{
    http.BaseAddress = new Uri("https://hacker-news.firebaseio.com/");
    http.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<BestStoriesService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/stories/best", async (int n, BestStoriesService svc, CancellationToken ct) =>
{
    // Hard limit to avoid abuse
    if (n < 1) return Results.BadRequest("n must be >= 1");
    if (n > 200) return Results.BadRequest("n must be <= 200");

    var result = await svc.GetBestAsync(n, ct);
    return Results.Ok(result);
})
.WithName("GetBestStories");

app.Run();