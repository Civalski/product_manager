using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductStore.Api.Configuration;
using ProductStore.Api.Identity;

namespace ProductStore.Api.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
{
    public const string PendingLoginPurpose = "pending_login";

    private readonly JwtOptions _options = jwtOptions.Value;

    public string CreateToken(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(_options.Key) || _options.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 caracteres.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (!string.IsNullOrEmpty(user.UserName))
            claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(Math.Max(1, _options.ExpireDays));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreatePendingLoginToken(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(_options.Key) || _options.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 caracteres.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new("purpose", PendingLoginPurpose),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (!string.IsNullOrEmpty(user.UserName))
            claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = Math.Clamp(_options.PendingExpireMinutes, 1, 60);
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: string.IsNullOrWhiteSpace(_options.PendingAudience)
                ? "ProductStore.Pending"
                : _options.PendingAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public PendingLoginPrincipal? ValidatePendingLoginToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_options.Key) || _options.Key.Length < 32)
            return null;

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var pendingAudience = string.IsNullOrWhiteSpace(_options.PendingAudience)
            ? "ProductStore.Pending"
            : _options.PendingAudience;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = pendingAudience,
            IssuerSigningKey = key,
            NameClaimType = ClaimTypes.NameIdentifier,
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            var purpose = principal.FindFirst("purpose")?.Value;
            if (purpose != PendingLoginPurpose)
                return null;

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            var name = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
            return new PendingLoginPrincipal(userId, name);
        }
        catch
        {
            return null;
        }
    }
}

public sealed record PendingLoginPrincipal(string UserId, string? UserName);
