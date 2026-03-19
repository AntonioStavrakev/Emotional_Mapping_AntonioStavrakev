using Emotional_Mapping.Application.DTOs;
using FluentValidation;

namespace Emotional_Mapping.Application.Validation;

public class FeedbackDtoValidator : AbstractValidator<FeedbackDto>
{
    public FeedbackDtoValidator()
    {
        RuleFor(x => x.GeneratedMapId)
            .NotEmpty().WithMessage("Моля, посочи идентификатор на генерираната карта.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .When(x => x.Rating.HasValue)
            .WithMessage("Оценката трябва да е между 1 и 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment))
            .WithMessage("Коментарът може да е до 500 символа.");
        
    }
}