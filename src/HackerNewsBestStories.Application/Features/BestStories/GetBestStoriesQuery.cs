namespace HackerNewsBestStories.Application.Features.BestStories;

using HackerNewsBestStories.Application.DTOs;
using HackerNewsBestStories.Core.Common.Results;
using MediatR;

public sealed record GetBestStoriesQuery(int N) : IRequest<Result<IReadOnlyList<StoryDto>>>;
