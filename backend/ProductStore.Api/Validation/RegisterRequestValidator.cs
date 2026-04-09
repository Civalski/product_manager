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
            .MinimumLength(8)
            .MaximumLength(128)
            .Must(p => p.Any(char.IsDigit) && p.Any(c => !char.IsLetterOrDigit(c)))
            .WithMessage("A palavra-passe deve incluir pelo menos um número e um carácter especial.");
    }
}
