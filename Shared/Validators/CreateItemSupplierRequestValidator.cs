using FluentValidation;
using MenuManager.Shared.DTOs;

namespace MenuManager.Shared.Validators;

public class CreateItemSupplierRequestValidator : AbstractValidator<CreateItemSupplierRequest>
{
    public CreateItemSupplierRequestValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0);

        RuleFor(x => x.SupplierId)
            .GreaterThan(0);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0);
    }
}
