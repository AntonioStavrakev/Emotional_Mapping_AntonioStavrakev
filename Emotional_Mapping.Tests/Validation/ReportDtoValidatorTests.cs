using Emotional_Mapping.Application.DTOs;
using Emotional_Mapping.Application.Validation;

namespace Emotional_Mapping.Tests.Validation;

public class ReportDtoValidatorTests
{
    private readonly ReportDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenEmotionalPointIsProvided_ShouldPass()
    {
        var dto = new ReportDto
        {
            EmotionalPointId = Guid.NewGuid(),
            Reason = "Неподходящо съдържание."
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPlaceIsProvided_ShouldPass()
    {
        var dto = new ReportDto
        {
            PlaceId = Guid.NewGuid(),
            Reason = "Невалидно място."
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNoTargetIsProvided_ShouldHaveModelError()
    {
        var dto = new ReportDto
        {
            Reason = "Проблем."
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == string.Empty);
    }

    [Fact]
    public void Validate_WhenReasonIsEmpty_ShouldHaveReasonError()
    {
        var dto = new ReportDto
        {
            PlaceId = Guid.NewGuid(),
            Reason = string.Empty
        };

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReportDto.Reason));
    }
}
