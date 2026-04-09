using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductStore.Api.Data;

namespace ProductStore.Api.Services;

public sealed class TenantAppDbContextFactory(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    ILogger<TenantAppDbContextFactory> logger) : ITenantAppDbContextFactory
{
    private static readonly ConcurrentDictionary<string, bool> MigratedTenants = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TenantMigrationLocks = new(StringComparer.OrdinalIgnoreCase);

    public AppDbContext CreateDbContext()
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Contexto de tenant requer utilizador autenticado.");

        var dataDir = TenantPaths.ResolveUsersDataDirectory(webHostEnvironment);
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, $"{userId}.db");
        var connectionString = $"Data Source={dbPath}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning))
            .Options;

        EnsureTenantMigrated(userId, options);

        return new AppDbContext(options);
    }

    private void EnsureTenantMigrated(string userId, DbContextOptions<AppDbContext> options)
    {
        if (MigratedTenants.ContainsKey(userId))
            return;

        var migrationLock = TenantMigrationLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        migrationLock.Wait();

        try
        {
            if (MigratedTenants.ContainsKey(userId))
                return;

            using var db = new AppDbContext(options);
            db.Database.Migrate();
            CategoryNormalizedNameSync.AfterMigrate(db);
            MigratedTenants[userId] = true;
            logger.LogInformation("Migrações aplicadas ao tenant {UserId}", userId);
        }
        catch (Exception ex)
        {
            MigratedTenants.TryRemove(userId, out _);
            logger.LogError(ex, "Falha ao migrar base de dados do tenant {UserId}", userId);
            throw;
        }
        finally
        {
            migrationLock.Release();
        }
    }
}
