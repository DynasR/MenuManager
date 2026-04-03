using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class MenuPlanRequestValidator : AbstractValidator<MenuPlanRequest>
{
    public MenuPlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2020);

        RuleFor(x => x.CustomerId)
            .GreaterThan(0);
    }
}
