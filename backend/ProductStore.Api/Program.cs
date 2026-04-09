using FluentValidation;

using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;

using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Diagnostics;

using DotNetEnv;

using ProductStore.Api.Configuration;

using ProductStore.Api.Data;

using ProductStore.Api.Middleware;

using ProductStore.Api.Models;

using ProductStore.Api.Services;



DotEnvBootstrap.TryLoadDotEnvFiles();

var builder = WebApplication.CreateBuilder(args);

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

var dbPath = Path.Combine(dataDir, "products.db");

var connectionString = $"Data Source={dbPath}";



builder.Services.AddDbContext<AppDbContext>(options =>

    options.UseSqlite(connectionString).ConfigureWarnings(w =>

        w.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning)));



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



var app = builder.Build();



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



// Em Development, não redirecionar HTTP→HTTPS: o front (Vite) chama http://localhost:5127

// e o redirect quebraria fetch/CORS com "failed to fetch".

if (!app.Environment.IsDevelopment())

    app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();



using (var scope = app.Services.CreateScope())

{

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();



    if (!await db.Categories.AnyAsync())

    {

        db.Categories.AddRange(

            new Category { Id = Guid.NewGuid(), Name = "Acessório" },

            new Category { Id = Guid.NewGuid(), Name = "Eletrônico" });

        await db.SaveChangesAsync();

    }



    if (!await db.Products.AnyAsync())

    {

        var catAcc = await db.Categories.FirstAsync(c => c.Name == "Acessório");

        var catEl = await db.Categories.FirstAsync(c => c.Name == "Eletrônico");

        db.Products.AddRange(

            new Product

            {

                Id = Guid.NewGuid(),

                Sku = "7891910000197",

                Name = "Produto exemplo (GTIN Cosmos)",

                Description = "Uso diário",

                Price = 89.90m,

                Stock = 12,

                CategoryId = catAcc.Id

            },

            new Product

            {

                Id = Guid.NewGuid(),

                Sku = "7891910000203",

                Name = "Produto exemplo embalagem (GTIN Cosmos)",

                Description = "Categoria eletrônico com preço válido",

                Price = 120m,

                Stock = 5,

                CategoryId = catEl.Id

            });

        await db.SaveChangesAsync();

    }

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

