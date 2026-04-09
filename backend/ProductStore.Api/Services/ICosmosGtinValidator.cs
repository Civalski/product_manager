using ProductStore.Api.DTOs;

namespace ProductStore.Api.Services;

/// <summary>
/// Valida o SKU como GTIN/EAN na API Bluesoft Cosmos quando o token Cosmos está configurado.
/// </summary>
public interface ICosmosGtinValidator
{
    Task ValidateAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>Obtém os dados do produto na Cosmos; exige token configurado.</summary>
    Task<CosmosGtinProductDto> FetchProductAsync(string sku, CancellationToken cancellationToken = default);
}
