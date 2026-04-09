using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductStore.Api.Data;

namespace ProductStore.Api.Services;

public sealed class TenantAppDbContextFactory(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment) : ITenantAppDbContextFactory
{
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

        return new AppDbContext(options);
    }
}
