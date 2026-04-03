using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class MealSlotRequestValidator : AbstractValidator<MealSlotRequest>
{
    public MealSlotRequestValidator()
    {
        RuleFor(x => x.MealType)
            .IsInEnum();
    }
}
