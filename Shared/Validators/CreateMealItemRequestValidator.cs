using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class CreateMealItemRequestValidator : AbstractValidator<CreateMealItemRequest>
{
    public CreateMealItemRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.ItemId.HasValue && x.ItemId > 0) || (x.RecipeId.HasValue && x.RecipeId > 0))
            .WithMessage("Either ItemId or RecipeId must be provided.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Unit)
            .IsInEnum();
    }
}
