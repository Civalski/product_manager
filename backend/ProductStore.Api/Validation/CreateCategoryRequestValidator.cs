using FluentValidation;
using ProductStore.Api.DTOs;

namespace ProductStore.Api.Validation;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome da categoria é obrigatório.")
            .MaximumLength(128);
    }
}
