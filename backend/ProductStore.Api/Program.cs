using System.Security.Claims;
using System.Text;

using FluentValidation;

using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.HttpOverrides;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Microsoft.AspNetCore.RateLimiting;

using Microsoft.IdentityModel.Tokens;

using System.Threading.RateLimiting;

using System.Net.Http.Headers;

using System.Text.Json.Serialization;

using DotNetEnv;

using ProductStore.Api.Authentication;

using ProductStore.Api.Configuration;

using ProductStore.Api.Data;

using ProductStore.Api.Http;

using ProductStore.Api.Identity;

using ProductStore.Api.Middleware;

using ProductStore.Api.Services;



DotEnvBootstrap.TryLoadDotEnvFiles();

var builder = WebApplication.CreateBuilder(args);

// Render / Docker / reverse proxy: X-Forwarded-For e RemoteIpAddress corretos para rate limit e Turnstile siteverify.
// ForwardLimit = 1: Render usa um proxy; aceitar apenas o último salto impede clientes de falsificar o IP via X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Origens do front em produção (Vercel, etc.): lista separada por vírgulas.
// Env: CORS_ORIGINS ou Cors__AllowedOrigins (ex.: https://app.vercel.app,https://*.vercel.app não é suportado — liste cada URL de preview se precisar).
static string[] ParseCorsOrigins(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return [];
    return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

var corsOriginsRaw = builder.Configuration["Cors:AllowedOrigins"];
if (string.IsNullOrWhiteSpace(corsOriginsRaw))
    corsOriginsRaw = Environment.GetEnvironmentVariable("CORS_ORIGINS");

var prodCorsOrigins = ParseCorsOrigins(corsOriginsRaw);



// Em Development, SQLite fica em `<repo>/data/` para o `dotnet watch` não registar cada escrita (-wal/-shm) dentro do projeto.
var dataDir = builder.Environment.IsDevelopment()
    ? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data"))
    : Path.Combine(builder.Environment.ContentRootPath, "Data");

Directory.CreateDirectory(dataDir);

var usersDataDir = Path.Combine(dataDir, "users");

Directory.CreateDirectory(usersDataDir);

var identityDbPath = Path.Combine(dataDir, "identity.db");

var identityConnectionString = $"Data Source={identityDbPath}";



builder.Services.AddDbContext<AppIdentityDbContext>(options =>

    options.UseSqlite(identityConnectionString).ConfigureWarnings(w =>

        w.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning)));



builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantAppDbContextFactory, TenantAppDbContextFactory>();

builder.Services.AddScoped<AppDbContext>(sp =>

    sp.GetRequiredService<ITenantAppDbContextFactory>().CreateDbContext());



builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>

    {

        options.User.RequireUniqueEmail = false;

        options.Password.RequiredLength = 8;

        options.Password.RequireDigit = true;

        options.Password.RequireLowercase = true;

        options.Password.RequireUppercase = false;

        options.Password.RequireNonAlphanumeric = true;

    })

    .AddEntityFrameworkStores<AppIdentityDbContext>()

    .AddDefaultTokenProviders();



builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection(TurnstileOptions.SectionName));

builder.Services.AddHttpClient(nameof(TurnstileVerificationService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddScoped<ITurnstileVerificationService, TurnstileVerificationService>();

builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddScoped<TenantDatabaseProvisioner>();



var isIntegrationTesting = builder.Environment.IsEnvironment("IntegrationTesting");

if (isIntegrationTesting)

{

    builder.Services.AddAuthentication(options =>

        {

            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;

            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;

        })

        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

}

else

{

    var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

    var jwtKey = jwtSection["Key"] ?? string.Empty;

    builder.Services.AddAuthentication(options =>

        {

            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        })

        .AddJwtBearer(options =>

        {

            options.TokenValidationParameters = new TokenValidationParameters

            {

                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSection["Issuer"],

                ValidAudience = jwtSection["Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                NameClaimType = ClaimTypes.NameIdentifier,

                RoleClaimType = ClaimTypes.Role,

            };

        });

}



builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection(CosmosOptions.SectionName));

builder.Services.AddHttpClient<ICosmosGtinValidator, CosmosGtinValidator>((sp, client) =>

{

    var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosOptions>>().Value;

    var baseUrl = (string.IsNullOrWhiteSpace(o.BaseUrl) ? "https://api.cosmos.bluesoft.com.br" : o.BaseUrl.TrimEnd('/')) + "/";

    client.BaseAddress = new Uri(baseUrl);

    client.Timeout = TimeSpan.FromSeconds(20);

    client.DefaultRequestHeaders.UserAgent.Clear();

    client.DefaultRequestHeaders.UserAgent.ParseAdd(

        string.IsNullOrWhiteSpace(o.UserAgent) ? "ProductStore.Api/1.0" : o.UserAgent.Trim());

    client.DefaultRequestHeaders.Accept.Clear();

    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

});



builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<ICategoryService, CategoryService>();



builder.Services.AddControllers()

    .AddJsonOptions(o =>

    {

        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));

    });

builder.Services.AddOpenApi();



builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();



builder.Services.Configure<ApiBehaviorOptions>(options =>

{

    options.InvalidModelStateResponseFactory = context =>

    {

        var log = context.HttpContext.RequestServices

            .GetRequiredService<ILoggerFactory>()

            .CreateLogger("ModelValidation");

        log.LogWarning(

            "Validação rejeitada em {Method} {Path}",

            context.HttpContext.Request.Method,

            context.HttpContext.Request.Path);

        return new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));

    };

});



builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();



builder.Services.AddCors(o =>

{

    o.AddPolicy("DevFront", p => p

        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")

        .AllowAnyHeader()

        .AllowAnyMethod());

    if (prodCorsOrigins.Length > 0)

    {

        o.AddPolicy("ProdFront", p => p

            .WithOrigins(prodCorsOrigins)

            .AllowAnyHeader()

            .AllowAnyMethod());

    }

});



builder.Services.AddRateLimiter(options =>

{

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, _) =>

    {

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))

            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        await ValueTask.CompletedTask;

    };

    options.AddPolicy("auth-login", httpContext =>

    {

        var ip = ClientIpResolver.GetClientIpAddress(httpContext) ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions

        {

            PermitLimit = 10,

            Window = TimeSpan.FromMinutes(1),

            QueueLimit = 0,

            AutoReplenishment = true,

        });

    });

    options.AddPolicy("auth-register", httpContext =>

    {

        var ip = ClientIpResolver.GetClientIpAddress(httpContext) ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions

        {

            PermitLimit = 5,

            Window = TimeSpan.FromMinutes(1),

            QueueLimit = 0,

            AutoReplenishment = true,

        });

    });

});



JwtOptions.ThrowIfJwtKeyUnsafeForEnvironment(builder.Environment, builder.Configuration, isIntegrationTesting);

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())

{

    app.MapOpenApi();

    app.UseCors("DevFront");

}

else if (prodCorsOrigins.Length > 0)

{

    app.UseCors("ProdFront");

}



app.UseExceptionHandler();



app.Use(async (context, next) =>

{

    var sw = System.Diagnostics.Stopwatch.StartNew();

    await next();

    sw.Stop();

    var status = context.Response.StatusCode;

    if (status < 400)

        return;

    var log = context.RequestServices.GetRequiredService<ILoggerFactory>()

        .CreateLogger("HttpRequestSummary");

    if (status >= 500)

        log.LogError(

            "{Method} {Path}{Query} -> {StatusCode} ({ElapsedMs}ms)",

            context.Request.Method,

            context.Request.Path.Value,

            context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,

            status,

            sw.ElapsedMilliseconds);

    else

        log.LogWarning(

            "{Method} {Path}{Query} -> {StatusCode} ({ElapsedMs}ms)",

            context.Request.Method,

            context.Request.Path.Value,

            context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,

            status,

            sw.ElapsedMilliseconds);

});



app.UseRateLimiter();



// Em Development, não redirecionar HTTP→HTTPS: o front (Vite) chama http://localhost:5127

// e o redirect quebraria fetch/CORS com "failed to fetch".

if (!app.Environment.IsDevelopment())

    app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



using (var scope = app.Services.CreateScope())

{

    var identityDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

    await identityDb.Database.MigrateAsync();

}



app.Run();



// Permite WebApplicationFactory<Program> nos testes de integração.

public partial class Program { }



file static class DotEnvBootstrap

{

    public static void TryLoadDotEnvFiles()

    {

        try

        {

            var apiDir = ResolveApiProjectDirectory();

            var repoDir = Path.GetFullPath(Path.Combine(apiDir, "..", ".."));

            var cwd = Directory.GetCurrentDirectory();

            var paths = new[]

            {

                Path.Combine(repoDir, ".env"),

                Path.Combine(apiDir, ".env"),

                Path.Combine(cwd, ".env")

            }.Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).ToArray();

            if (paths.Length > 0)

            {

                Env.LoadMulti(paths);

            }

            else if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))

            {

                Console.Error.WriteLine(

                    "Aviso: nenhum arquivo .env encontrado. Procure em: " +

                    $"{repoDir}, {apiDir}, {cwd}. Copie .env.example para .env e preencha Cosmos__Token.");

            }

        }

        catch (Exception ex)

        {

            Console.Error.WriteLine($"Aviso: falha ao carregar .env — {ex.Message}");

        }

    }



    private static string ResolveApiProjectDirectory()

    {

        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)

        {

            if (File.Exists(Path.Combine(dir.FullName, "ProductStore.Api.csproj")))

                return dir.FullName;

            dir = dir.Parent;

        }

        return Directory.GetCurrentDirectory();

    }

}

