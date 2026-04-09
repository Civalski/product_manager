using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductStore.Api.Data;
using ProductStore.Api.Services;

namespace ProductStore.Api.Tests;

internal sealed class TestTenantAppDbContextFactory(SqliteConnection connection) : ITenantAppDbContextFactory
{
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning))
            .Options;
        return new AppDbContext(options);
    }
}
