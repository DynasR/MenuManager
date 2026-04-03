using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => x.Phone != null);

        RuleFor(x => x.Siret)
            .MaximumLength(14)
            .Matches(@"^\d+$").WithMessage("Siret must contain digits only.")
            .When(x => !string.IsNullOrEmpty(x.Siret));

        RuleFor(x => x.PostalCode)
            .MaximumLength(10)
            .When(x => x.PostalCode != null);
    }
}
