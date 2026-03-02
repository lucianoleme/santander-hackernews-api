# Santander HackerNews API

A small .NET 8 Web API that returns the top n Hacker News stories ranked by score in descending order.

This project focuses on a clean and minimal design, safe upstream usage to avoid overloading Hacker News and a small set of unit tests that lock the core behavior.

## Features

- GET endpoint to retrieve best stories at /api/stories/best?n=10
- Results sorted by score descending
- Filters only items of type story
- Converts Hacker News Unix time in seconds into ISO 8601 string
- Provides a safe fallback URL when the external URL is missing
- In-memory caching plus bounded concurrency to reduce load on the Hacker News API
- Unit tests for ranking filtering caching and URL fallback

## Tech stack

- .NET 8
- Minimal APIs without Controllers
- HttpClient using typed client
- IMemoryCache
- xUnit

## How to run

Prerequisites:
- .NET SDK 8.x

From the solution root:

dotnet restore  
dotnet run --project Santander.HackerNews.Api

The API will start on HTTPS and HTTP endpoints. If this is the first time you run it locally you may be prompted to trust the ASP.NET Core development certificate.

Swagger UI is enabled in Development. Open the Swagger page shown in the console output.

## Endpoint

GET /api/stories/best?n={n}

Returns the top n stories as a JSON array of StoryDto.

Query parameter:
- n is required and represents the number of stories to return
- n must be between 1 and 200 to avoid abuse

Example request:

https://localhost:7286/api/stories/best?n=10

Response schema StoryDto:

- title: string
- uri: string
- postedBy: string
- time: string in ISO 8601 format
- score: number
- commentCount: number

## Architecture overview

The code is split into three simple areas.

API boundary:
- Minimal API handler in Program.cs
- Validates input
- Calls the application service
- Returns HTTP results

Application service:
- BestStoriesService orchestrates the workflow
- Fetches best story IDs
- Fetches item details
- Filters items of type story
- Maps to StoryDto
- Sorts by score descending
- Takes the top n items

Infrastructure:
- HackerNewsClient is a thin HTTP adapter for the official Hacker News API
- AsyncKeyedLocker prevents cache stampedes

## Why Minimal APIs

Minimal APIs keep the HTTP boundary thin and readable for small services. The route handler plays the same role as a controller action by binding query parameters resolving services from dependency injection and serializing the result to JSON.

## Avoiding overload on Hacker News

The project includes a minimal but effective set of controls to avoid hammering the upstream API under burst traffic.

In-memory caching:
- Best story IDs are cached for a short TTL
- Individual item results are cached longer
- The ranked list is cached briefly to reduce recomputation

Singleflight per cache key:
- AsyncKeyedLocker ensures only one request rebuilds the same cache key
- Other requests wait and reuse the computed result

Bounded outbound concurrency:
- A SemaphoreSlim caps concurrent upstream calls
- Prevents a single request from triggering hundreds of parallel HTTP calls

These choices are intentionally simple and proportional to the scope of the exercise.

## Testing

Run tests from the solution root:

dotnet test

Testing approach:
- Unit tests target BestStoriesService behavior
- A hand-rolled fake FakeHackerNewsClient implements IHackerNewsClient
- Tests control returned IDs and items and verify outputs and interaction counts
- No real network calls are made in unit tests

Current coverage includes:
- Ranking by score descending and top n selection
- Filtering of non-story items
- Cache behavior to avoid repeated upstream calls
- URL fallback behavior when item URL is missing

## Key design decisions

- StoryDto is the external contract returned by the API and is not coupled to a specific use case name
- IHackerNewsClient exists for testability
- Production uses HackerNewsClient while tests inject a fake implementation
- Infrastructure payload types are kept internal to avoid leaking external API shapes
- File scoped namespaces are used for clarity and reduced indentation

## Future IMPROVEMENTS intentionally deferred

These are valuable in production but out of scope for a small coding exercise.

- Background refresh using IHostedService
- Retry with jitter and circuit breaker
- HTTP response caching at the edge layer
- Metrics tracing and advanced logging
- Distributed cache for multi-instance deployments

## Project structure

Santander.HackerNews.Api
- Program.cs
- Models/StoryDto.cs
- Services/BestStoriesService.cs
- Infrastructure/HackerNewsClient.cs
- Infrastructure/IHackerNewsClient.cs
- Infrastructure/AsyncKeyedLocker.cs

Santander.HackerNews.Api.Tests
- Fakes/FakeHackerNewsClient.cs
- Services/BestStoriesService.RankingTests.cs
- Services/BestStoriesService.CachingTests.cs

## Notes

This service depends on the public Hacker News API. Availability and data shape are controlled by the upstream service. 
The hard limit of n less than or equal to 200 is a simple abuse prevention measure.
