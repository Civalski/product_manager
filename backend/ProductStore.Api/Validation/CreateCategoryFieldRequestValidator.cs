using FluentValidation;

using ProductStore.Api.DTOs;



namespace ProductStore.Api.Validation;



public class CreateCategoryFieldRequestValidator : AbstractValidator<CreateCategoryFieldRequest>

{

    public CreateCategoryFieldRequestValidator()

    {

        RuleFor(x => x.Name)

            .NotEmpty().WithMessage("Nome do campo é obrigatório.")

            .MaximumLength(128);

    }

}
