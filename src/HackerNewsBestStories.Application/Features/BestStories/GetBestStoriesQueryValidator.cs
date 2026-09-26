namespace HackerNewsBestStories.Application.Features.BestStories;

using FluentValidation;

public sealed class GetBestStoriesQueryValidator : AbstractValidator<GetBestStoriesQuery>
{
    public GetBestStoriesQueryValidator()
    {
        RuleFor(x => x.StoryCount)
            .InclusiveBetween(1, 500).WithMessage("Story count must be between 1 and 500.");
    }
}
