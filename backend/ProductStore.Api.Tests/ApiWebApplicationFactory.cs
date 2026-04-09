using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ProductStore.Api.Data;
using ProductStore.Api.Domain;
using ProductStore.Api.Models;
using ProductStore.Api.Services;

namespace ProductStore.Api.Tests;

/// <summary>
/// Host de teste: SQLite em memória para Identity e tenant, autenticação de teste, stub Cosmos.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _tenantConnection = new("DataSource=:memory:");
    private readonly SqliteConnection _identityConnection = new("DataSource=:memory:");

    static ApiWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTesting");
        Environment.SetEnvironmentVariable("TestAuth__Enabled", "true");
    }

    public ApiWebApplicationFactory()
    {
        _tenantConnection.Open();
        _identityConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppIdentityDbContext>));
            services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlite(_identityConnection));

            services.RemoveAll(typeof(ITenantAppDbContextFactory));
            services.AddScoped<ITenantAppDbContextFactory>(_ => new TestTenantAppDbContextFactory(_tenantConnection));

            services.RemoveAll<ICosmosGtinValidator>();
            services.AddSingleton<ICosmosGtinValidator, NoOpCosmosGtinValidator>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        identityDb.Database.Migrate();

        var tenantFactory = scope.ServiceProvider.GetRequiredService<ITenantAppDbContextFactory>();
        using var tenantDb = tenantFactory.CreateDbContext();
        tenantDb.Database.Migrate();
        CategoryNormalizedNameSync.AfterMigrate(tenantDb);

        if (!tenantDb.Categories.Any())
        {
            tenantDb.Categories.AddRange(
                new Category { Id = Guid.NewGuid(), Name = "Acessório", NormalizedName = CategoryRules.NormalizeCategoryName("Acessório") },
                new Category { Id = Guid.NewGuid(), Name = "Eletrônico", NormalizedName = CategoryRules.NormalizeCategoryName("Eletrônico") });
            tenantDb.SaveChanges();
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tenantConnection.Dispose();
            _identityConnection.Dispose();
        }

        base.Dispose(disposing);
    }
}
