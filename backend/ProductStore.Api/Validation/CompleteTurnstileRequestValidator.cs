using FluentValidation;
using ProductStore.Api.DTOs;

namespace ProductStore.Api.Validation;

public sealed class CompleteTurnstileRequestValidator : AbstractValidator<CompleteTurnstileRequest>
{
    public CompleteTurnstileRequestValidator()
    {
        RuleFor(x => x.PendingToken).NotEmpty();
        RuleFor(x => x.TurnstileToken).NotEmpty();
    }
}
