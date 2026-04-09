using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductStore.Api.Data;

namespace ProductStore.Api.Services;

public sealed class TenantDatabaseProvisioner(IWebHostEnvironment env)
{
    public async Task CreateAndMigrateTenantDatabaseAsync(string userId, CancellationToken cancellationToken = default)
    {
        var dataDir = TenantPaths.ResolveUsersDataDirectory(env);
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, $"{userId}.db");
        var connectionString = $"Data Source={dbPath}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning))
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
        CategoryNormalizedNameSync.AfterMigrate(db);
    }
}
