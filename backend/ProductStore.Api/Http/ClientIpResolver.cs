namespace ProductStore.Api.Http;

/// <summary>
/// Resolve o IP do visitante depois do <c>ForwardedHeadersMiddleware</c>.
/// Assim evitamos confiar diretamente em headers encaminhados que podem ser manipulados pelo cliente.
/// </summary>
public static class ClientIpResolver
{
    public static string? GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString();
    }
}
