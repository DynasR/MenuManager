using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class DuplicateMonthRequestValidator : AbstractValidator<DuplicateMonthRequest>
{
    public DuplicateMonthRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.SourceYear).GreaterThan(0);
        RuleFor(x => x.SourceMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.TargetYear).GreaterThan(0);
        RuleFor(x => x.TargetMonth).InclusiveBetween(1, 12);
        RuleFor(x => x)
            .Must(x => !(x.SourceYear == x.TargetYear && x.SourceMonth == x.TargetMonth))
            .WithMessage("Source and target months must be different.");
    }
}
