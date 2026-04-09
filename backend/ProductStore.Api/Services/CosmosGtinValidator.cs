using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductStore.Api.Configuration;
using ProductStore.Api.DTOs;
using ProductStore.Api.Exceptions;
namespace ProductStore.Api.Services;

public sealed class CosmosGtinValidator(
    HttpClient http,
    IOptions<CosmosOptions> options,
    ILogger<CosmosGtinValidator> logger) : ICosmosGtinValidator
{
    private static readonly JsonSerializerOptions CosmosJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly CosmosOptions _options = options.Value;

    public async Task ValidateAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
            return;

        var digits = string.Concat(sku.Where(char.IsAsciiDigit));
        if (digits.Length is < 8 or > 14)
            throw new InvalidGtinSkuException(sku);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"gtins/{digits}.json");
        request.Headers.TryAddWithoutValidation("X-Cosmos-Token", _options.Token);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            await HandleStatusOrThrowAsync(response, digits, readBodyOnError: true, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CosmosApiException("Tempo esgotado ao consultar a API Cosmos.", 504);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Falha de rede ao consultar Cosmos para GTIN {Gtin}", digits);
            throw new CosmosApiException("Não foi possível contatar a API Cosmos.", 503);
        }
    }

    public async Task<CosmosGtinProductDto> FetchProductAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
            throw new CosmosNotConfiguredException();

        var digits = string.Concat(sku.Where(char.IsAsciiDigit));
        if (digits.Length is < 8 or > 14)
            throw new InvalidGtinSkuException(sku);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"gtins/{digits}.json");
        request.Headers.TryAddWithoutValidation("X-Cosmos-Token", _options.Token);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
                await HandleStatusOrThrowAsync(response, digits, readBodyOnError: true, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payloadJson = UnwrapCosmosPayloadJson(json);
            CosmosGtinProductDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<CosmosGtinProductDto>(payloadJson, CosmosJsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "JSON inválido da Cosmos para GTIN {Gtin}", digits);
                throw new CosmosApiException("Resposta inválida da API Cosmos.", 502);
            }

            if (dto is null)
                throw new CosmosApiException("Resposta inválida da API Cosmos.", 502);

            return dto;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CosmosApiException("Tempo esgotado ao consultar a API Cosmos.", 504);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Falha de rede ao consultar Cosmos para GTIN {Gtin}", digits);
            throw new CosmosApiException("Não foi possível contatar a API Cosmos.", 503);
        }
    }

    private async Task HandleStatusOrThrowAsync(
        HttpResponseMessage response,
        string digits,
        bool readBodyOnError,
        CancellationToken cancellationToken)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                return;
            case HttpStatusCode.NotFound:
                throw new CosmosProductNotFoundException(digits);
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                throw new CosmosApiException("Token da API Cosmos inválido ou sem permissão.", (int)response.StatusCode);
            case HttpStatusCode.TooManyRequests:
                throw new CosmosApiException("Limite de requisições da API Cosmos excedido. Tente novamente mais tarde.", 429);
            default:
                if (!readBodyOnError)
                    throw new CosmosApiException($"A API Cosmos retornou o status {(int)response.StatusCode}.", (int)response.StatusCode);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Cosmos retornou {Status} para GTIN {Gtin}: {Body}",
                    (int)response.StatusCode,
                    digits,
                    body.Length > 500 ? body[..500] : body);
                throw new CosmosApiException($"A API Cosmos retornou o status {(int)response.StatusCode}.", (int)response.StatusCode);
        }
    }

    /// <summary>Alguns clientes envolvem o produto em <c>data</c>; a API HTTP costuma retornar o objeto na raiz.</summary>
    private static string UnwrapCosmosPayloadJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            using var doc = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return json;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                return data.GetRawText();
            if (root.TryGetProperty("product", out var product) && product.ValueKind == JsonValueKind.Object)
                return product.GetRawText();
        }
        catch (JsonException)
        {
            return json;
        }

        return json;
    }
}
