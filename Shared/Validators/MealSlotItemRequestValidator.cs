using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class MealSlotItemRequestValidator : AbstractValidator<MealSlotItemRequest>
{
    public MealSlotItemRequestValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0);
    }
}
