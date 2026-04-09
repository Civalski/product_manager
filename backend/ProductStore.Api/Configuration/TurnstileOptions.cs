namespace ProductStore.Api.Configuration;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>Chave secreta do widget (siteverify). Não expor ao cliente.</summary>
    public string SecretKey { get; set; } = string.Empty;
}
