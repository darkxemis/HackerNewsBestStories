namespace HackerNewsBestStories.UnitTests.Features.BestStories;

using FluentAssertions;
using HackerNewsBestStories.Application.Features.BestStories;

public sealed class GetBestStoriesQueryValidatorTests
{
    private readonly GetBestStoriesQueryValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(500)]
    public void Accepts_story_counts_inside_the_allowed_range(int storyCount)
    {
        var result = _validator.Validate(new GetBestStoriesQuery(storyCount));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public void Rejects_story_counts_outside_the_allowed_range(int storyCount)
    {
        var result = _validator.Validate(new GetBestStoriesQuery(storyCount));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Story count must be between 1 and 500.");
    }

    [Fact]
    public void Reports_the_story_count_as_the_failing_property()
    {
        var result = _validator.Validate(new GetBestStoriesQuery(0));

        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(GetBestStoriesQuery.StoryCount));
    }
}
