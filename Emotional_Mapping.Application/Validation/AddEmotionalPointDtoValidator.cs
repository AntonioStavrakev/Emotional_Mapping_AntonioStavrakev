using Emotional_Mapping.Application.DTOs;
using FluentValidation;

namespace Emotional_Mapping.Application.Validation;

public class AddEmotionalPointDtoValidator : AbstractValidator<AddEmotionalPointDto>
{
    public AddEmotionalPointDtoValidator()
    {
        RuleFor(x => x.CityId).NotEmpty().WithMessage("Моля, избери град.");

        RuleFor(x => x.Lat).InclusiveBetween(-90, 90).WithMessage("Невалидна ширина (Lat).");
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180).WithMessage("Невалидна дължина (Lng).");

        RuleFor(x => x.Intensity)
            .InclusiveBetween(1, 5)
            .WithMessage("Интензитетът трябва да е от 1 до 5.");

        RuleFor(x => x.Title)
            .MaximumLength(120)
            .When(x => x.Title != null)
            .WithMessage("Заглавието може да е до 120 символа.");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note != null)
            .WithMessage("Описанието може да е до 500 символа.");
    }
}