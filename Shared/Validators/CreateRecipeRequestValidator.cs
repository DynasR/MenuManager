using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class CreateRecipeRequestValidator : AbstractValidator<CreateRecipeRequest>
{
    public CreateRecipeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.BaseServings)
            .GreaterThan(0);
    }
}
