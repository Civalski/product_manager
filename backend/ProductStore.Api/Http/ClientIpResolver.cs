namespace ProductStore.Api.Http;

/// <summary>
/// Resolve o IP do visitante quando a API está atrás de proxy (Render, Nginx, Cloudflare).
/// O Turnstile siteverify usa <c>remoteip</c>; se for o IP interno do proxy, a validação falha.
/// </summary>
public static class ClientIpResolver
{
    public static string? GetClientIpAddress(HttpContext context)
    {
        var cf = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cf))
            return cf.Trim();

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(first))
                return first;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
