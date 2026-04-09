using FluentValidation;

using ProductStore.Api.Domain;

using ProductStore.Api.DTOs;



namespace ProductStore.Api.Validation;



public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>

{

    public UpdateProductRequestValidator()

    {

        RuleFor(x => x.Sku)

            .NotEmpty().WithMessage("SKU é obrigatório.")

            .MaximumLength(64);



        When(x => x.SkuSource == SkuSource.CosmosGtin, () =>

        {

            RuleFor(x => x.Sku)

                .Must(HasValidGtinDigits)

                .WithMessage("Para GTIN real (Bluesoft), informe um código com 8 a 14 dígitos numéricos.");

        });



        RuleFor(x => x.Name)

            .NotEmpty().WithMessage("Nome é obrigatório.")

            .MaximumLength(256);



        RuleFor(x => x.Description)

            .MaximumLength(2000);



        RuleFor(x => x.CategoryId)

            .NotEmpty().WithMessage("Categoria é obrigatória.");



        RuleFor(x => x.Stock)

            .GreaterThan(0).WithMessage("Estoque deve ser maior que zero.");



        RuleFor(x => x.Price)

            .GreaterThanOrEqualTo(0).WithMessage("Preço não pode ser negativo.");

    }



    private static bool HasValidGtinDigits(string sku)

    {

        var digits = string.Concat(sku.Where(char.IsAsciiDigit));

        return digits.Length is >= 8 and <= 14;

    }

}

