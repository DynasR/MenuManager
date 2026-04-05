using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class CreateDailyMenuRequestValidator : AbstractValidator<CreateDailyMenuRequest>
{
    public CreateDailyMenuRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .GreaterThan(0);
    }
}
