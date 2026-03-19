using Emotional_Mapping.Application.DTOs;
using FluentValidation;

namespace Emotional_Mapping.Application.Validation;

public class GenerateMapRequestDtoValidator : AbstractValidator<GenerateMapRequestDto>
{
    public GenerateMapRequestDtoValidator()
    {
        RuleFor(x => x.CityId)
            .NotEmpty().WithMessage("Моля, избери град.");

        RuleFor(x => x.QueryText)
            .MaximumLength(700).WithMessage("Текстът може да е до 700 символа.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.QueryText) || x.SelectedEmotion != null)
            .WithMessage("Въведи настроение (текст) или избери емоция.");

        RuleFor(x => x.RadiusMeters)
            .InclusiveBetween(50, 50000)
            .When(x => x.RadiusMeters.HasValue)
            .WithMessage("Радиусът трябва да е между 50 и 50000 метра.");
    }
}