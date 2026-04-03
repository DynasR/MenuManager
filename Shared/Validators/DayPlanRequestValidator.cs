using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class DayPlanRequestValidator : AbstractValidator<DayPlanRequest>
{
    public DayPlanRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty();
    }
}
