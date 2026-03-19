using Emotional_Mapping.Application.DTOs;
using FluentValidation;

namespace Emotional_Mapping.Application.Validation;

public class ReportDtoValidator : AbstractValidator<ReportDto>
{
    public ReportDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Моля, опиши причината.")
            .MaximumLength(500).WithMessage("Причината може да е до 500 символа.");

        RuleFor(x => x)
            .Must(x => x.EmotionalPointId.HasValue || x.PlaceId.HasValue)
            .WithMessage("Трябва да избереш какво докладваш (точка или място).");
    }
}