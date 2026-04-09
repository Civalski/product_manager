using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ProductStore.Api.Tests;

/// <summary>Backup sem produtos na base do tenant de teste.</summary>
public sealed class ProductExportEmptyTests : IClassFixture<ApiWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public ProductExportEmptyTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Export_SemProdutos_Retorna400_ComDetalhe()
    {
        var res = await _client.PostAsJsonAsync("/api/products/export", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Não existem produtos para guardar no backup.", problem!.Detail);
    }

    public void Dispose() => _client.Dispose();
}
