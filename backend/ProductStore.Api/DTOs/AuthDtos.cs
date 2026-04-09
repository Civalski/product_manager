namespace ProductStore.Api.DTOs;

public sealed class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Campo armadilha (honeypot); deve permanecer vazio.</summary>
    public string? Website { get; set; }
}

public sealed class LoginRequest
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Campo armadilha (honeypot); deve permanecer vazio.</summary>
    public string? Website { get; set; }
}

/// <summary>Resposta do login antes da verificação Turnstile; não concede acesso à API de produtos.</summary>
public sealed class LoginPendingResponse
{
    public string PendingToken { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public DateTimeOffset PendingExpiresAtUtc { get; set; }
}

public sealed class CompleteTurnstileRequest
{
    public string PendingToken { get; set; } = string.Empty;

    public string TurnstileToken { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
