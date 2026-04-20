using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Validation;

namespace Emotional_Mapping.Tests.Validation;

public class GenerateMapRequestDtoValidatorTests
{
    private readonly GenerateMapRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenCityAndQueryAreProvided_ShouldPass()
    {
        var dto = new GenerateMapRequestDto
        {
            CityId = Guid.NewGuid(),
            QueryText = "Искам спокойни места за разходка."
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenCityAndEmotionAreProvided_ShouldPass()
    {
        var dto = new GenerateMapRequestDto
        {
            CityId = Guid.NewGuid(),
            SelectedEmotion = Domain.Enums.EmotionType.Calm
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenCityIsEmpty_ShouldHaveCityError()
    {
        var dto = new GenerateMapRequestDto
        {
            CityId = Guid.Empty,
            QueryText = "Текст"
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GenerateMapRequestDto.CityId));
    }

    [Fact]
    public void Validate_WhenQueryAndEmotionAreMissing_ShouldHaveModelError()
    {
        var dto = new GenerateMapRequestDto
        {
            CityId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == string.Empty);
    }

    [Fact]
    public void Validate_WhenRadiusIsOutsideAllowedRange_ShouldHaveRadiusError()
    {
        var dto = new GenerateMapRequestDto
        {
            CityId = Guid.NewGuid(),
            QueryText = "Текст",
            RadiusMeters = 10
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GenerateMapRequestDto.RadiusMeters));
    }
}
