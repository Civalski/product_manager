using FluentValidation;
using ProductStore.Api.DTOs;

namespace ProductStore.Api.Validation;

public class ProductListQueryValidator : AbstractValidator<ProductListQuery>
{
    private static readonly string[] AllowedStockFilters = ["available", "low"];

    public ProductListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page deve ser >= 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize deve estar entre 1 e 100.");

        RuleFor(x => x.Search)
            .MaximumLength(256).WithMessage("Termo de busca não pode exceder 256 caracteres.");

        RuleFor(x => x.Sku)
            .MaximumLength(64).WithMessage("Filtro de SKU não pode exceder 64 caracteres.");

        RuleFor(x => x.Name)
            .MaximumLength(256).WithMessage("Filtro de nome não pode exceder 256 caracteres.");

        RuleFor(x => x.StockFilter)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStockFilters.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("StockFilter deve ser: available ou low.");
    }
}
