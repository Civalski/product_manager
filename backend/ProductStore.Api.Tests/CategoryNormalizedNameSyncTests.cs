using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProductStore.Api.Data;
using ProductStore.Api.Services;
using Xunit;

namespace ProductStore.Api.Tests;

public sealed class CategoryNormalizedNameSyncTests
{
    [Fact]
    public void AfterMigrate_QuandoNormalizacaoColide_LancaErroExplicito()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        db.Categories.AddRange(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Café",
                NormalizedName = "café"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Cafe",
                NormalizedName = "cafe"
            });
        db.SaveChanges();

        var ex = Assert.Throws<InvalidOperationException>(() => CategoryNormalizedNameSync.AfterMigrate(db));

        Assert.Contains("Conflito ao normalizar categorias existentes", ex.Message, StringComparison.Ordinal);
        Assert.Contains("\"Café\"", ex.Message, StringComparison.Ordinal);
        Assert.Contains("\"Cafe\"", ex.Message, StringComparison.Ordinal);
    }
}
