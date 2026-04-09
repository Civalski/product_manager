using ProductStore.Api.Data;

namespace ProductStore.Api.Services;

/// <summary>
/// Cria um <see cref="AppDbContext"/> para o utilizador autenticado (ficheiro SQLite por tenant).
/// </summary>
public interface ITenantAppDbContextFactory
{
    AppDbContext CreateDbContext();
}
