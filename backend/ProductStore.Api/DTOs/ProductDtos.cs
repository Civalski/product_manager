using System.Text.Json.Nodes;

using ProductStore.Api.Domain;



namespace ProductStore.Api.DTOs;



/// <summary>Dados do produto real (Bluesoft Cosmos), espelhados em colunas ao salvar com GTIN.</summary>
public record RealSkuFromCosmos(

    string? CommercialDescription,

    string? Gtin,

    string? Thumbnail,

    string? BrandName,

    string? BrandPicture,

    decimal? AvgPrice,

    decimal? MaxPrice,

    decimal? MinPrice,

    string? PriceLabel,

    string? NcmCode,

    string? NcmDescription,

    string? GpcCode,

    string? GpcDescription,

    double? GrossWeightGrams,

    double? NetWeightGrams,

    double? Width,

    double? Height,

    double? Length);



public record ProductResponse(

    Guid Id,

    string Sku,

    string Name,

    string? Description,

    decimal Price,

    decimal PaidAmount,

    int Stock,

    Guid CategoryId,

    string Category,

    JsonNode? CosmosMetadata = null,

    RealSkuFromCosmos? RealSku = null);



public class CreateProductRequest

{

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal PaidAmount { get; set; }

    public int Stock { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>internal = código interno; cosmosGtin = GTIN validado na Bluesoft com preenchimento automático.</summary>
    public SkuSource SkuSource { get; set; } = SkuSource.Internal;

}



public class UpdateProductRequest

{

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal PaidAmount { get; set; }

    public int Stock { get; set; }

    public Guid CategoryId { get; set; }

    public SkuSource SkuSource { get; set; } = SkuSource.Internal;

}



public class ProductListQuery

{

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    /// <summary>Busca em nome, SKU e descrição (sem diferenciar maiúsculas).</summary>

    public string? Search { get; set; }

    public string? Sku { get; set; }

    public string? Name { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    /// <summary>available | low (estoque baixo ≤5)</summary>

    public string? StockFilter { get; set; }

}



public record PagedProductsResponse(

    IReadOnlyList<ProductResponse> Items,

    int Page,

    int PageSize,

    int TotalCount);



/// <summary>Resposta ao gerar backup JSON de todos os produtos (conteúdo para guardar no cliente).</summary>

public record ProductExportResult(

    string FileName,

    int ProductCount,

    DateTimeOffset ExportedAtUtc,

    string Json);



public record CategoryResponse(Guid Id, string Name);



public class CreateCategoryRequest

{

    public string Name { get; set; } = string.Empty;

}

