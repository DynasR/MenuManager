using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class RecipeIngredientRequestValidator : AbstractValidator<RecipeIngredientRequest>
{
    public RecipeIngredientRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Unit)
            .IsInEnum();

        RuleFor(x => x.Order)
            .GreaterThan(0);
    }
}
