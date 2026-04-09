using FluentValidation;
using Microsoft.Extensions.Hosting;
using ProductStore.Api.DTOs;

namespace ProductStore.Api.Validation;

public sealed class CompleteTurnstileRequestValidator : AbstractValidator<CompleteTurnstileRequest>
{
    public CompleteTurnstileRequestValidator(IHostEnvironment hostEnvironment)
    {
        RuleFor(x => x.PendingToken).NotEmpty();
        RuleFor(x => x.TurnstileToken).NotEmpty().Unless(_ => hostEnvironment.IsDevelopment());
    }
}
