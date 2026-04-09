namespace ProductStore.Api.Models;



public class Product

{

    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    /// <summary>JSON retornado pela Bluesoft Cosmos (GTIN real), quando aplicável.</summary>
    public string? CosmosMetadataJson { get; set; }

    // --- Campos normalizados do SKU real (preenchidos na consulta Cosmos) ---

    /// <summary>Nome comercial completo retornado pela Cosmos (<c>description</c>).</summary>
    public string? CosmosCommercialDescription { get; set; }

    /// <summary>GTIN numérico (redundante com <see cref="Sku"/> quando origem é GTIN).</summary>
    public string? CosmosGtin { get; set; }

    public string? CosmosThumbnailUrl { get; set; }

    public string? CosmosBrandName { get; set; }

    public string? CosmosBrandPictureUrl { get; set; }

    public decimal? CosmosAvgPrice { get; set; }

    public decimal? CosmosMaxPrice { get; set; }

    public decimal? CosmosMinPrice { get; set; }

    public string? CosmosPriceLabel { get; set; }

    public string? CosmosNcmCode { get; set; }

    public string? CosmosNcmDescription { get; set; }

    public string? CosmosGpcCode { get; set; }

    public string? CosmosGpcDescription { get; set; }

    public double? CosmosGrossWeightGrams { get; set; }

    public double? CosmosNetWeightGrams { get; set; }

    public double? CosmosWidth { get; set; }

    public double? CosmosHeight { get; set; }

    public double? CosmosLength { get; set; }

}

