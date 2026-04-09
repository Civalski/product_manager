namespace ProductStore.Api.Configuration;

/// <summary>Configuração da API Bluesoft Cosmos (documentação: https://cosmos.bluesoft.com.br/api).</summary>
public sealed class CosmosOptions
{
    public const string SectionName = "Cosmos";

    /// <summary>Token da Bluesoft Cosmos (header X-Cosmos-Token). Vazio = não valida na API.</summary>
    public string? Token { get; set; }

    public string BaseUrl { get; set; } = "https://api.cosmos.bluesoft.com.br";

    /// <summary>Opcional: alguns contratos Cosmos exigem o User-Agent indicado no painel.</summary>
    public string? UserAgent { get; set; }
}
