using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ProductStore.Api.Configuration;
using ProductStore.Api.DTOs;
using ProductStore.Api.Http;
using ProductStore.Api.Identity;
using ProductStore.Api.Services;

namespace ProductStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TenantDatabaseProvisioner tenantProvisioner,
    JwtTokenService jwtTokenService,
    ITurnstileVerificationService turnstileVerification,
    IOptions<JwtOptions> jwtOptions,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<CompleteTurnstileRequest> completeTurnstileValidator,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            logger.LogWarning("Registo rejeitado: honeypot preenchido");
            return BadRequest(new ProblemDetails
            {
                Title = "Pedido inválido.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Não foi possível concluir o pedido.",
            });
        }

        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var remoteIp = ClientIpResolver.GetClientIpAddress(HttpContext);
        var turnstileOk = await turnstileVerification.VerifyAsync(request.TurnstileToken ?? "", remoteIp, cancellationToken);
        if (!turnstileOk)
            return Problem(detail: "Verificação Cloudflare inválida ou expirada. Tente novamente.", statusCode: StatusCodes.Status400BadRequest);

        var userName = request.UserName.Trim();
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@users.local",
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var state = new ModelStateDictionary();
            foreach (var err in createResult.Errors)
                state.AddModelError(nameof(RegisterRequest.UserName), err.Description);
            return ValidationProblem(state);
        }

        try
        {
            await tenantProvisioner.CreateAndMigrateTenantDatabaseAsync(user.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao criar base de dados do tenant para {UserId}", user.Id);
            await userManager.DeleteAsync(user);
            return Problem(
                detail: "Não foi possível criar a base de dados do utilizador. Tente novamente.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var token = jwtTokenService.CreateToken(user);
        var expires = DateTimeOffset.UtcNow.AddDays(Math.Max(1, jwtOptions.Value.ExpireDays));

        logger.LogInformation("Registo concluído para utilizador {UserName}", userName);

        return Ok(new AuthResponse
        {
            Token = token,
            UserName = userName,
            ExpiresAtUtc = expires,
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(LoginPendingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginPendingResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            logger.LogWarning("Login rejeitado: honeypot preenchido");
            return BadRequest(new ProblemDetails
            {
                Title = "Pedido inválido.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Não foi possível concluir o pedido.",
            });
        }

        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var userName = request.UserName.Trim();
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
            return Unauthorized();

        var valid = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!valid.Succeeded)
            return Unauthorized();

        var pendingMinutes = Math.Clamp(jwtOptions.Value.PendingExpireMinutes, 1, 60);
        var pendingToken = jwtTokenService.CreatePendingLoginToken(user);
        var pendingExpires = DateTimeOffset.UtcNow.AddMinutes(pendingMinutes);

        return Ok(new LoginPendingResponse
        {
            PendingToken = pendingToken,
            UserName = userName,
            PendingExpiresAtUtc = pendingExpires,
        });
    }

    [HttpPost("complete-turnstile")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> CompleteTurnstile(
        [FromBody] CompleteTurnstileRequest request,
        CancellationToken cancellationToken)
    {
        await completeTurnstileValidator.ValidateAndThrowAsync(request, cancellationToken);

        var pending = jwtTokenService.ValidatePendingLoginToken(request.PendingToken);
        if (pending is null)
            return Unauthorized();

        var remoteIp = ClientIpResolver.GetClientIpAddress(HttpContext);
        var turnstileOk = await turnstileVerification.VerifyAsync(request.TurnstileToken, remoteIp, cancellationToken);
        if (!turnstileOk)
            return Problem(detail: "Verificação Cloudflare inválida ou expirada. Tente novamente.", statusCode: StatusCodes.Status400BadRequest);

        var user = await userManager.FindByIdAsync(pending.UserId);
        if (user is null)
            return Unauthorized();

        try
        {
            await tenantProvisioner.CreateAndMigrateTenantDatabaseAsync(user.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao preparar base de dados do tenant para {UserId} durante o login", user.Id);
            return Problem(
                detail: "Não foi possível preparar a base de dados do utilizador. Tente novamente.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var token = jwtTokenService.CreateToken(user);
        var expires = DateTimeOffset.UtcNow.AddDays(Math.Max(1, jwtOptions.Value.ExpireDays));
        var name = user.UserName ?? pending.UserName ?? string.Empty;

        logger.LogInformation("Login concluído após Turnstile para {UserName}", name);

        return Ok(new AuthResponse
        {
            Token = token,
            UserName = name,
            ExpiresAtUtc = expires,
        });
    }
}
