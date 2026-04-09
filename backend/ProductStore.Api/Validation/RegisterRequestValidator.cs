using FluentValidation;
using ProductStore.Api.DTOs;

namespace ProductStore.Api.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(64)
            .Matches(@"^[\p{L}\p{N}_\-.]+$")
            .WithMessage("O nome de utilizador só pode conter letras, números, _, - e .");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(128);
    }
}
