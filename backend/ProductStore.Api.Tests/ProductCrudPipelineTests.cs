using System.Net;
using System.Net.Http.Json;
using ProductStore.Api.DTOs;
using Xunit;

namespace ProductStore.Api.Tests;

/// <summary>
/// Pipeline: criar produto → listar com filtro → atualizar → excluir → confirmar 404.
/// </summary>
public sealed class ProductCrudPipelineTests : IClassFixture<ApiWebApplicationFactory>, IDisposable
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductCrudPipelineTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Pipeline_Criar_Filtrar_Atualizar_Excluir_Conclui_Com_Sucesso()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        Assert.NotNull(categories);
        var category = Assert.Single(categories!, c => c.Name == "Acessório");

        var marker = Guid.NewGuid().ToString("N")[..8];
        var skuOriginal = $"E2E-{marker}";
        var nameOriginal = $"Produto pipeline {marker}";

        var createBody = new CreateProductRequest
        {
            Sku = skuOriginal,
            Name = nameOriginal,
            Description = $"Descrição teste {marker}",
            Price = 19.90m,
            Stock = 10,
            CategoryId = category.Id
        };

        var createRes = await _client.PostAsJsonAsync("/api/products", createBody);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);
        Assert.Equal(skuOriginal, created!.Sku);
        Assert.Equal(nameOriginal, created.Name);

        var listUrl = $"/api/products?search={Uri.EscapeDataString(marker)}&page=1&pageSize=20";
        var listRes = await _client.GetAsync(listUrl);
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var page = await listRes.Content.ReadFromJsonAsync<PagedProductsResponse>();
        Assert.NotNull(page);
        Assert.Contains(page!.Items, p => p.Id == created.Id);

        var skuNovo = $"{skuOriginal}-U";
        var nameNovo = $"{nameOriginal} atualizado";
        var updateBody = new UpdateProductRequest
        {
            Sku = skuNovo,
            Name = nameNovo,
            Description = $"Atualizado {marker}",
            Price = 29.90m,
            Stock = 7,
            CategoryId = category.Id
        };

        var putRes = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updateBody);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var updated = await putRes.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(updated);
        Assert.Equal(skuNovo, updated!.Sku);
        Assert.Equal(nameNovo, updated.Name);
        Assert.Equal(29.90m, updated.Price);

        var listSkuRes = await _client.GetAsync($"/api/products?sku={Uri.EscapeDataString(skuNovo)}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listSkuRes.StatusCode);
        var pageSku = await listSkuRes.Content.ReadFromJsonAsync<PagedProductsResponse>();
        Assert.NotNull(pageSku);
        Assert.Contains(pageSku!.Items, p => p.Id == created.Id);

        var delRes = await _client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

        var getRes = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task Export_Post_DevolveJsonComTodosOsProdutos()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        Assert.NotNull(categories);
        var category = Assert.Single(categories!, c => c.Name == "Acessório");

        var marker = Guid.NewGuid().ToString("N")[..8];
        var createBody = new CreateProductRequest
        {
            Sku = $"EXP-{marker}",
            Name = $"Export test {marker}",
            Description = "Produto para teste de exportação JSON",
            Price = 9.99m,
            Stock = 3,
            CategoryId = category.Id
        };

        var createRes = await _client.PostAsJsonAsync("/api/products", createBody);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var exportRes = await _client.PostAsJsonAsync("/api/products/export", new { });
        Assert.Equal(HttpStatusCode.OK, exportRes.StatusCode);
        var export = await exportRes.Content.ReadFromJsonAsync<ProductExportResult>();
        Assert.NotNull(export);
        Assert.True(export!.ProductCount >= 1);
        Assert.False(string.IsNullOrWhiteSpace(export.FileName));
        Assert.EndsWith(".json", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(export.Json));
        Assert.Contains(marker, export.Json, StringComparison.Ordinal);
    }

    public void Dispose() => _client.Dispose();
}
