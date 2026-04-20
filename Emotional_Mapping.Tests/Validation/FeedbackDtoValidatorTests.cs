using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Validation;

namespace Emotional_Mapping.Tests.Validation;

public class FeedbackDtoValidatorTests
{
    private readonly FeedbackDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = new FeedbackDto
        {
            GeneratedMapId = Guid.NewGuid(),
            Rating = 5,
            Comment = "Полезна карта."
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenGeneratedMapIdIsMissing_ShouldHaveMapError()
    {
        var dto = new FeedbackDto
        {
            GeneratedMapId = Guid.Empty,
            Rating = 5
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(FeedbackDto.GeneratedMapId));
    }

    [Fact]
    public void Validate_WhenRatingIsOutsideRange_ShouldHaveRatingError()
    {
        var dto = new FeedbackDto
        {
            GeneratedMapId = Guid.NewGuid(),
            Rating = 6
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(FeedbackDto.Rating));
    }

    [Fact]
    public void Validate_WhenCommentIsTooLong_ShouldHaveCommentError()
    {
        var dto = new FeedbackDto
        {
            GeneratedMapId = Guid.NewGuid(),
            Comment = new string('a', 501)
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(FeedbackDto.Comment));
    }
}
