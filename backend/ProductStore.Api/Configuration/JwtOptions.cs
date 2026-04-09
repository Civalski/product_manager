namespace ProductStore.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ProductStore";

    public string Audience { get; set; } = "ProductStore";

    /// <summary>Audience do JWT de login pendente (Turnstile). Deve ser diferente de <see cref="Audience"/> para o Bearer não aceitar este token nas APIs.</summary>
    public string PendingAudience { get; set; } = "ProductStore.Pending";

    /// <summary>Chave simétrica (HMAC). Mínimo recomendado: 32 caracteres.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpireDays { get; set; } = 7;

    /// <summary>Validade do token de login pendente (minutos).</summary>
    public int PendingExpireMinutes { get; set; } = 10;
}
