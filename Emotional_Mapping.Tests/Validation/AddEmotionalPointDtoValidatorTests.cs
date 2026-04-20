using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Validation;
using Emotional_Mapping.Domain.Enums;

namespace Emotional_Mapping.Tests.Validation;

public class AddEmotionalPointDtoValidatorTests
{
    private readonly AddEmotionalPointDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = CreateValidDto();

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenCityIsMissing_ShouldHaveCityError()
    {
        var dto = CreateValidDto();
        dto.CityId = Guid.Empty;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddEmotionalPointDto.CityId));
    }

    [Fact]
    public void Validate_WhenEmotionIsMissing_ShouldHaveEmotionError()
    {
        var dto = CreateValidDto();
        dto.Emotion = null;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddEmotionalPointDto.Emotion));
    }

    [Fact]
    public void Validate_WhenEmotionValueIsInvalid_ShouldHaveEmotionError()
    {
        var dto = CreateValidDto();
        dto.Emotion = (EmotionType)999;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddEmotionalPointDto.Emotion));
    }

    [Fact]
    public void Validate_WhenLatitudeIsInvalid_ShouldHaveLatError()
    {
        var dto = CreateValidDto();
        dto.Lat = 95;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddEmotionalPointDto.Lat));
    }

    [Fact]
    public void Validate_WhenIntensityIsInvalid_ShouldHaveIntensityError()
    {
        var dto = CreateValidDto();
        dto.Intensity = 7;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddEmotionalPointDto.Intensity));
    }

    private static AddEmotionalPointDto CreateValidDto()
    {
        return new AddEmotionalPointDto
        {
            CityId = Guid.NewGuid(),
            Lat = 42.6977,
            Lng = 23.3219,
            Emotion = EmotionType.Joy,
            Intensity = 3,
            Title = "Тест",
            Note = "Тестова бележка"
        };
    }
}
