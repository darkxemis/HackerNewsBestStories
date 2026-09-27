<div align="center">

# 📰 Hacker News Best Stories

**A small REST API that serves the best *n* stories from Hacker News, sorted by score.**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![MediatR](https://img.shields.io/badge/MediatR-CQRS-8B5CF6)](https://github.com/jbogard/MediatR)
[![Polly](https://img.shields.io/badge/Polly-Resilience-C72C48)](https://github.com/App-vNext/Polly)
[![Serilog](https://img.shields.io/badge/Serilog-Structured-00B3C7?logo=serilog&logoColor=white)](https://serilog.net/)
[![Scalar](https://img.shields.io/badge/Scalar-API_Docs-2B7A9E?logo=scalar&logoColor=white)](https://scalar.com/)
[![xUnit](https://img.shields.io/badge/xUnit-Tests-512BD4?logo=xunit&logoColor=white)](https://xunit.net/)

</div>

> **A quick note on the shape of this thing:** the brief didn't mention architecture at all, so I built it the way I normally work — Clean Architecture with four projects (Core, Application, Infrastructure, WebApi), CQRS slices per feature and the result pattern for expected failures. The only piece I left out is persistence: there's no database in this exercise, everything comes from the Hacker News API, so the "domain" is just a `Story` record and Infrastructure holds an HTTP client instead of EF Core.

---

## 🚀 Getting started

### Option 1 — Docker

```bash
git clone https://github.com/darkxemis/HackerNewsBestStories.git
cd HackerNewsBestStories
docker compose up -d --build
```

The API is up on **http://localhost:5093**. There is no database or any other service to set up. Stop it with `docker compose down`.

### Option 2 — running it locally

All you need is the [.NET 10 SDK](https://dotnet.microsoft.com/download):

```bash
git clone https://github.com/darkxemis/HackerNewsBestStories.git
cd HackerNewsBestStories
dotnet run --project src/HackerNewsBestStories.WebApi
```

Same address, **http://localhost:5093**. If you'd rather use the HTTPS profile, add `--launch-profile https` and hit https://localhost:7136 instead.

### What to open

| | |
| --- | --- |
| API (Docker or local) | http://localhost:5093 |
| Interactive docs (Scalar) | http://localhost:5093/scalar/v1 |
| OpenAPI document | http://localhost:5093/openapi/v1.json |

Both options run in the Development environment by default, which is where the docs UI is enabled.

A first call from the terminal:

```bash
curl "http://localhost:5093/api/v1/top-stories?storyCount=5"
```

---

## 📝 The brief

The exercise, condensed:

> Using ASP.NET Core, implement a RESTful API to retrieve the details of the best *n* stories from the Hacker News API, as determined by their score, where *n* is specified by the caller to the API. [...] The API must serve a large number of requests efficiently without overloading the Hacker News API.

Hard requirements are the JSON shape of the response and not melting Hacker News. The response contract asked for:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

Everything else — architecture, error format, cache TTL, naming — was left to me.

## ✨ What it does

- `GET /api/v1/top-stories?storyCount=10` returns the best stories, highest score first.
- Keeps a comfortable distance between us and Hacker News:
  - a concurrency gate caps parallel calls to **8** at any moment,
  - **Polly** adds rate limiting, timeouts, retries and a circuit breaker,
  - a **5-minute cache** absorbs the bursts — concurrent requests for the same `storyCount` are coalesced into a single upstream call, and failures are never cached.
- Input validation runs in the MediatR pipeline, before the handler ever sees the query.
- Anything unexpected is caught by a global exception handler, so clients always get JSON back instead of an HTML error page.
- Structured logs go to the console and to daily files under `logs/`.

---

## 📡 API

| Method | Route | Description |
| ------ | ----- | ----------- |
| `GET` | `/api/v1/top-stories?storyCount=10` | The best stories, sorted by score descending |

`storyCount` is the *n* from the brief. I renamed it because `?n=10` tells you nothing at a glance, while `storyCount` reads straight away in a query string, in Scalar or in a code review. Same meaning: optional (defaults to **10**), accepted range **1–500**, anything outside it is a `400`.

### Error handling

Every failure comes back as **ProblemDetails (RFC 7807)**, plus a couple of fields I add on top:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "tag": "validation.failed",
  "traceId": "00-0e42a71e6f2b4bbfa1c1f18a13f70112-0000000000000001-00",
  "errors": { "StoryCount": "Story count must be between 1 and 500." }
}
```

I've been using this shape in my own projects and I kept it here on purpose: `status` and `title` cover the generic case, but the `tag` is what a client should actually switch on. Tags are stable keys, so the frontend can translate messages itself instead of parsing (and re-localising) human text — that has saved us more than once. `traceId` points at the matching Serilog entry when something breaks.

| Status | Tag | When |
| ------ | --- | ---- |
| `400` | `validation.failed` | `storyCount` outside the 1–500 range |
| `502` | `hackerNews.unavailable` | Hacker News timed out, rate limited us, or the circuit breaker is open |
| `500` | `server.internalError` | Anything unexpected, caught by `GlobalExceptionHandler` (exception detail only in Development) |

---

## 🏗️ Architecture

```
GET /api/v1/top-stories?storyCount=10
   │
   ▼
BestStoriesEndpoints            binds the query string, maps Result<T> to a status code
   │
   ▼
ValidationBehavior              FluentValidation: storyCount 1..500
   │
   ▼
GetBestStoriesQueryHandler
   │   HybridCache  best-stories:{storyCount}  ·  5 min  ·  anti-stampede
   ▼
HackerNewsApiClient             Polly: rate limit → total timeout → retry → circuit breaker
   │   concurrency gate  ·  max 8 parallel calls
   ▼
https://hacker-news.firebaseio.com
```

| Concern | Implementation |
| -------- | -------------- |
| API style | ASP.NET Core **Minimal APIs**, endpoint group `/api/v1` |
| Use cases | **CQRS** through MediatR 14 (`GetBestStoriesQuery` + handler) |
| Validation | **FluentValidation** 12 wired as a pipeline behaviour |
| Caching | **HybridCache**, key `best-stories:{storyCount}`, 5 min TTL, stampede protection |
| Resilience | **Microsoft.Extensions.Http.Resilience** (standard handler) + `SemaphoreSlim` gate |
| Business errors | **`Result<T>` + `Error`** mapped to ProblemDetails by the endpoint |
| Unexpected errors | Global **`IExceptionHandler`** → ProblemDetails + `traceId` |
| Logging | **Serilog** — console + daily rolling files + request logging |
| API docs | **Scalar** on top of native `Microsoft.AspNetCore.OpenApi` |

### Repository structure

```
HackerNewsBestStories/
├── HackerNewsBestStories.slnx
├── Directory.Build.props              # net10.0, nullable, implicit usings
├── Dockerfile
├── docker-compose.yml
├── src/
│   ├── HackerNewsBestStories.Core/            # Result, Error, ErrorTags, Story — zero dependencies
│   ├── HackerNewsBestStories.Application/     # features, DTOs, ports, cache configuration
│   │   └── Features/BestStories/              # query + handler + validator
│   ├── HackerNewsBestStories.Infrastructure/  # Hacker News client, concurrency gate, Polly, DI
│   └── HackerNewsBestStories.WebApi/
│       ├── Features/BestStories/              # endpoints
│       ├── Middleware/                        # ApiProblem, ResultExtensions, GlobalExceptionHandler
│       ├── DependencyInjection.cs             # OpenAPI document info + exception handling
│       └── Program.cs
└── tests/
    └── HackerNewsBestStories.UnitTests/       # xUnit + Moq + FluentAssertions
```

Dependencies only point inwards: `Core ← Application ← Infrastructure ← WebApi`.

---

## 🧰 Tech stack

- **.NET 10** (C# 14, nullable + implicit usings)
- ASP.NET Core Minimal APIs · MediatR 14 · FluentValidation 12
- HybridCache · Microsoft.Extensions.Http.Resilience (Polly) · Serilog · Scalar
- xUnit · Moq · FluentAssertions
- Docker / Docker Compose

## 🧪 Tests

```bash
dotnet test
```

16 tests covering score ordering, the requested count, deleted stories on Hacker News (dropped, not failed), a dead upstream (502), a repeat request served from cache without new upstream calls, the validation bounds and the exact JSON contract of the response.

---

## 📋 Assumptions

- **Range 1–500.** The brief never sets one; 500 stops a single request from pulling down Hacker News' entire best list. Outside the range → `400`.
- **Ordering.** `beststories.json` already arrives ordered by score, but I sort again by score descending (ties broken by the highest id) so the contract doesn't depend on upstream behaviour.
- **`commentCount` is `descendants`** — the total number of comments in the thread — and `0` when a story has none yet.
- **Stories with no external link** (Ask HN, polls) fall back to `https://news.ycombinator.com/item?id={id}` for `uri`.
- **Deleted or unavailable items** (`item/{id}` coming back empty) are dropped from the list instead of failing the request, so the response can come back shorter than `storyCount`.
- **Cache TTL** of 5 minutes per `storyCount`, configurable in `appsettings.json` under `Cache:ExpirationSeconds`.
- **No auth and no rate limit on our side** — it's a single read-only endpoint meant to be evaluated, not exposed to the internet.
- Timestamps are returned as ISO 8601 with their original offset.

## 🔮 Future improvements

- Cache each story by id and assemble the lists from it, so `storyCount=10` and `storyCount=11` stop duplicating the same items, and refresh entries in the background (stale-while-revalidate) instead of making requests wait on a cold key.
- Conditional requests (`ETag`) towards Hacker News for items we already hold.
- Rate limiting and an API key on our own endpoint if it ever goes public.
- OpenTelemetry: cache hit ratio, Hacker News latency, circuit-breaker state.
- Integration tests with `WebApplicationFactory` and a stubbed Hacker News server.
- Docker healthcheck and a non-root user in the final image.

---

<div align="center">

Made to be run in one command: `docker compose up -d --build`.

</div>
