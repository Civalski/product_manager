using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProductStore.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Valor padrão em <c>appsettings.json</c>; não pode ser usado fora de Development / testes de integração.</summary>
    public const string DevelopmentPlaceholderKey = "DEV_ONLY_CHANGE_IN_PRODUCTION_MINIMUM_32_CHARACTERS_LONG_KEY";

    public string Issuer { get; set; } = "ProductStore";

    public string Audience { get; set; } = "ProductStore";

    /// <summary>Audience do JWT de login pendente (Turnstile). Deve ser diferente de <see cref="Audience"/> para o Bearer não aceitar este token nas APIs.</summary>
    public string PendingAudience { get; set; } = "ProductStore.Pending";

    /// <summary>Chave simétrica (HMAC). Mínimo recomendado: 32 caracteres.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpireDays { get; set; } = 7;

    /// <summary>Validade do token de login pendente (minutos).</summary>
    public int PendingExpireMinutes { get; set; } = 10;

    /// <summary>
    /// Falha no arranque se JWT estiver inseguro em qualquer ambiente que não seja Development nem IntegrationTesting.
    /// </summary>
    public static void ThrowIfJwtKeyUnsafeForEnvironment(IHostEnvironment env, IConfiguration configuration, bool isIntegrationTesting)
    {
        if (env.IsDevelopment() || isIntegrationTesting)
            return;

        var key = configuration.GetSection(SectionName)["Key"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException(
                "Defina Jwt__Key (ou Jwt:Key) com pelo menos 32 caracteres secretos. Em produção/staging não use a chave de desenvolvimento do repositório.");
        }

        if (string.Equals(key.Trim(), DevelopmentPlaceholderKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Jwt__Key não pode ser a chave de placeholder do repositório. Gere um segredo forte (ex.: 48 bytes em Base64) e configure-o nas variáveis de ambiente do serviço.");
        }
    }
}
