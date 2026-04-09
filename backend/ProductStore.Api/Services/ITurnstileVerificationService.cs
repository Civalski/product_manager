namespace ProductStore.Api.Services;

public interface ITurnstileVerificationService
{
    Task<bool> VerifyAsync(string turnstileToken, string? remoteIp, CancellationToken cancellationToken);
}
