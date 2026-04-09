using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductStore.Api.Data;

namespace ProductStore.Api.Services;

public sealed class TenantDatabaseProvisioner(IWebHostEnvironment env, ILogger<TenantDatabaseProvisioner> logger)
{
    public async Task CreateAndMigrateTenantDatabaseAsync(string userId, CancellationToken cancellationToken = default)
    {
        var dataDir = TenantPaths.ResolveUsersDataDirectory(env);
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, $"{userId}.db");
        await MigrateDatabaseAsync(dbPath, cancellationToken);
    }

    public async Task MigrateExistingTenantDatabasesAsync(CancellationToken cancellationToken = default)
    {
        var dataDir = TenantPaths.ResolveUsersDataDirectory(env);
        Directory.CreateDirectory(dataDir);

        foreach (var dbPath in Directory.EnumerateFiles(dataDir, "*.db"))
        {
            await MigrateDatabaseAsync(dbPath, cancellationToken);
        }
    }

    private async Task MigrateDatabaseAsync(string dbPath, CancellationToken cancellationToken)
    {
        var connectionString = $"Data Source={dbPath}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning))
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
        CategoryNormalizedNameSync.AfterMigrate(db);
        logger.LogInformation("Migrações aplicadas à base do tenant em {DbPath}", dbPath);
    }
}
