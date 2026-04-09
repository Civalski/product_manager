using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductStore.Api.Configuration;

namespace ProductStore.Api.Services;

public sealed class TurnstileVerificationService(
    IHttpClientFactory httpClientFactory,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileVerificationService> logger) : ITurnstileVerificationService
{
    private readonly TurnstileOptions _options = options.Value;

    public async Task<bool> VerifyAsync(string turnstileToken, string? remoteIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            logger.LogWarning("Turnstile:SecretKey não configurada; não é possível validar o widget.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(turnstileToken))
            return false;

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("secret", _options.SecretKey.Trim()),
            new("response", turnstileToken.Trim()),
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
            pairs.Add(new("remoteip", remoteIp));

        var client = httpClientFactory.CreateClient(nameof(TurnstileVerificationService));
        using var content = new FormUrlEncodedContent(pairs);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao contactar o siteverify do Turnstile.");
            return false;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True)
                return true;

            if (doc.RootElement.TryGetProperty("error-codes", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var codes = string.Join(", ", errors.EnumerateArray().Select(e => e.GetString()));
                logger.LogInformation("Turnstile rejeitou o token. error-codes: {Codes}", codes);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resposta inválida do siteverify do Turnstile.");
            return false;
        }
    }
}
