using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProductStore.Api.Data;
using ProductStore.Api.Domain;
using ProductStore.Api.DTOs;
using ProductStore.Api.Exceptions;
using ProductStore.Api.Models;

namespace ProductStore.Api.Services;

public class ProductService(
    AppDbContext db,
    ICosmosGtinValidator cosmosGtinValidator,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ProductService> logger) : IProductService
{
    private const int MaxPageSize = 100;

    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions StoreCosmosMetaOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly JsonSerializerOptions ReadStoredCosmosOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        string skuNormalized;
        string name;
        string? description;
        decimal price;
        string? cosmosJson = null;
        CosmosGtinProductDto? cosmosDto = null;

        if (request.SkuSource == SkuSource.CosmosGtin)
        {
            cosmosDto = await cosmosGtinValidator.FetchProductAsync(request.Sku, cancellationToken);
            var digits = string.Concat(request.Sku.Where(char.IsAsciiDigit));
            skuNormalized = digits;
            name = string.IsNullOrWhiteSpace(request.Name)
                ? (cosmosDto.Description ?? digits)
                : request.Name.Trim();
            var userDesc = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            description = BuildCosmosDescription(cosmosDto, userDesc);
            price = request.Price > 0 ? request.Price : (cosmosDto.AvgPrice ?? 0m);
            cosmosJson = JsonSerializer.Serialize(cosmosDto, StoreCosmosMetaOptions);
        }
        else
        {
            skuNormalized = request.Sku.Trim();
            name = request.Name.Trim();
            description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            price = request.Price;
        }

        if (await SkuExistsAsync(skuNormalized, null, cancellationToken))
            throw new DuplicateSkuException(skuNormalized);

        var category = await db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new CategoryNotFoundException(request.CategoryId);

        EnsureElectronicsMinPrice(category.Name, price);

        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Sku = skuNormalized,
            Name = name,
            Description = description,
            Price = price,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            CosmosMetadataJson = cosmosJson,
        };

        if (request.SkuSource == SkuSource.CosmosGtin)
            ApplyCosmosRealSkuColumns(entity, cosmosDto!, skuNormalized);
        else
            ClearCosmosRealSkuColumns(entity);

        db.Products.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Falha ao persistir produto com SKU {Sku}", skuNormalized);
            throw new DuplicateSkuException(skuNormalized);
        }

        logger.LogInformation("Produto criado Id={ProductId} SKU={Sku}", entity.Id, entity.Sku);
        return Map(entity, category.Name);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        string skuNormalized;
        string name;
        string? description;
        decimal price;
        string? cosmosJson;
        CosmosGtinProductDto? cosmosDto = null;

        if (request.SkuSource == SkuSource.CosmosGtin)
        {
            cosmosDto = await cosmosGtinValidator.FetchProductAsync(request.Sku, cancellationToken);
            var digits = string.Concat(request.Sku.Where(char.IsAsciiDigit));
            skuNormalized = digits;
            name = string.IsNullOrWhiteSpace(request.Name)
                ? (cosmosDto.Description ?? digits)
                : request.Name.Trim();
            var userDesc = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            description = BuildCosmosDescription(cosmosDto, userDesc);
            price = request.Price > 0 ? request.Price : (cosmosDto.AvgPrice ?? 0m);
            cosmosJson = JsonSerializer.Serialize(cosmosDto, StoreCosmosMetaOptions);
        }
        else
        {
            skuNormalized = request.Sku.Trim();
            name = request.Name.Trim();
            description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            price = request.Price;
            cosmosJson = null;
        }

        if (await SkuExistsAsync(skuNormalized, id, cancellationToken))
            throw new DuplicateSkuException(skuNormalized);

        var category = await db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new CategoryNotFoundException(request.CategoryId);

        EnsureElectronicsMinPrice(category.Name, price);

        entity.Sku = skuNormalized;
        entity.Name = name;
        entity.Description = description;
        entity.Price = price;
        entity.Stock = request.Stock;
        entity.CategoryId = request.CategoryId;
        entity.CosmosMetadataJson = cosmosJson;

        if (request.SkuSource == SkuSource.CosmosGtin)
            ApplyCosmosRealSkuColumns(entity, cosmosDto!, skuNormalized);
        else
            ClearCosmosRealSkuColumns(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Falha ao atualizar produto Id={ProductId} SKU={Sku}", id, skuNormalized);
            throw new DuplicateSkuException(skuNormalized);
        }

        logger.LogInformation("Produto atualizado Id={ProductId} SKU={Sku}", id, entity.Sku);
        return Map(entity, category.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        db.Products.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Produto excluído Id={ProductId}", id);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return entity is null ? null : Map(entity, entity.Category.Name);
    }

    public async Task<PagedProductsResponse> ListAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : Math.Min(query.PageSize, MaxPageSize);

        var q = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(p =>
                p.Name.ToLower().Contains(s)
                || p.Sku.ToLower().Contains(s)
                || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(query.Sku))
        {
            var sku = query.Sku.Trim().ToLower();
            q = q.Where(p => p.Sku.ToLower().Contains(sku));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(name));
        }

        if (query.CategoryId is { } catId)
            q = q.Where(p => p.CategoryId == catId);

        if (query.MinPrice is { } minP)
            q = q.Where(p => p.Price >= minP);

        if (query.MaxPrice is { } maxP)
            q = q.Where(p => p.Price <= maxP);

        switch (query.StockFilter?.Trim().ToLowerInvariant())
        {
            case "available":
                q = q.Where(p => p.Stock > 0);
                break;
            case "out":
                q = q.Where(p => p.Stock == 0);
                break;
            case "low":
                q = q.Where(p => p.Stock > 0 && p.Stock <= 5);
                break;
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var pageIds = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var pageSet = pageIds.ToHashSet();
        var rows = await q
            .Where(p => pageSet.Contains(p.Id))
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);

        var order = pageIds.Select((pid, i) => (pid, i)).ToDictionary(x => x.pid, x => x.i);
        var items = rows
            .OrderBy(p => order[p.Id])
            .Select(p => Map(p, p.Category.Name))
            .ToList();

        logger.LogDebug(
            "Listagem de produtos Page={Page} PageSize={PageSize} Total={Total}",
            page,
            pageSize,
            totalCount);

        return new PagedProductsResponse(items, page, pageSize, totalCount);
    }

    public Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var trimmed = sku.Trim();
        var query = db.Products.AsNoTracking().Where(p => p.Sku == trimmed);
        if (excludeId is { } ex)
            query = query.Where(p => p.Id != ex);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<ProductExportResult> ExportAllToJsonAsync(CancellationToken cancellationToken = default)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Exportação requer utilizador autenticado.");

        var exportedAt = DateTimeOffset.UtcNow;
        var fileName = $"products_backup_{exportedAt:yyyyMMdd_HHmmss}.json";

        var entities = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var products = entities.Select(p => Map(p, p.Category.Name)).ToList();

        if (products.Count == 0)
            throw new NoProductsToExportException();

        var payload = new ProductsExportFilePayload(exportedAt, products.Count, products);
        var json = JsonSerializer.Serialize(payload, ExportJsonOptions);

        logger.LogInformation(
            "Backup JSON gerado: {ProductCount} produtos (ficheiro sugerido {FileName})",
            products.Count,
            fileName);

        return new ProductExportResult(fileName, products.Count, exportedAt, json);
    }

    private sealed record ProductsExportFilePayload(
        DateTimeOffset ExportedAtUtc,
        int ProductCount,
        IReadOnlyList<ProductResponse> Products);

    private static void EnsureElectronicsMinPrice(string categoryName, decimal price)
    {
        if (CategoryRules.IsElectronics(categoryName) && price < CategoryRules.ElectronicsMinPrice)
            throw new ElectronicsMinPriceException(CategoryRules.ElectronicsMinPrice);
    }

    private static string BuildCosmosDescription(CosmosGtinProductDto d, string? userExtra)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(d.Brand?.Name))
            lines.Add($"Marca: {d.Brand.Name}");
        if (d.Ncm is not null && (!string.IsNullOrWhiteSpace(d.Ncm.Code) || !string.IsNullOrWhiteSpace(d.Ncm.Description)))
            lines.Add($"NCM: {d.Ncm.Code} — {d.Ncm.Description}".TrimEnd(' ', '—'));
        if (d.Gpc is not null && (!string.IsNullOrWhiteSpace(d.Gpc.Code) || !string.IsNullOrWhiteSpace(d.Gpc.Description)))
            lines.Add($"GPC: {d.Gpc.Code} — {d.Gpc.Description}".TrimEnd(' ', '—'));
        if (d.NetWeight is > 0)
            lines.Add($"Peso líquido: {d.NetWeight} g");
        if (d.GrossWeight is > 0)
            lines.Add($"Peso bruto: {d.GrossWeight} g");
        if (d.Width is > 0 || d.Height is > 0 || d.Length is > 0)
            lines.Add(
                $"Dimensões (L×A×C): {d.Length ?? 0} × {d.Width ?? 0} × {d.Height ?? 0}".Trim());
        if (!string.IsNullOrWhiteSpace(d.PriceLabel))
            lines.Add($"Preço referência (Cosmos): {d.PriceLabel}");
        if (!string.IsNullOrWhiteSpace(d.Thumbnail))
            lines.Add($"Miniatura: {d.Thumbnail}");

        var body = string.Join(Environment.NewLine, lines);
        if (!string.IsNullOrWhiteSpace(userExtra))
            body = body.Length > 0 ? body + Environment.NewLine + Environment.NewLine + userExtra.Trim() : userExtra.Trim();

        const int maxLen = 2000;
        if (body.Length > maxLen)
            body = body[..maxLen];
        return body;
    }

    private static JsonNode? ParseCosmosMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ProductResponse Map(Product p, string categoryName) =>
        new(
            p.Id,
            p.Sku,
            p.Name,
            p.Description,
            p.Price,
            p.Stock,
            p.CategoryId,
            categoryName,
            ParseCosmosMetadata(p.CosmosMetadataJson),
            BuildRealSku(p));

    private static void ApplyCosmosRealSkuColumns(Product entity, CosmosGtinProductDto dto, string normalizedGtinDigits)
    {
        entity.CosmosCommercialDescription = dto.Description;
        entity.CosmosGtin = normalizedGtinDigits;
        entity.CosmosThumbnailUrl = dto.Thumbnail;
        entity.CosmosBrandName = dto.Brand?.Name;
        entity.CosmosBrandPictureUrl = dto.Brand?.Picture;
        entity.CosmosAvgPrice = dto.AvgPrice;
        entity.CosmosMaxPrice = dto.MaxPrice;
        entity.CosmosMinPrice = dto.MinPrice;
        entity.CosmosPriceLabel = dto.PriceLabel;
        entity.CosmosNcmCode = dto.Ncm?.Code;
        entity.CosmosNcmDescription = !string.IsNullOrWhiteSpace(dto.Ncm?.Description)
            ? dto.Ncm.Description
            : dto.Ncm?.FullDescription;
        entity.CosmosGpcCode = dto.Gpc?.Code;
        entity.CosmosGpcDescription = dto.Gpc?.Description;
        entity.CosmosGrossWeightGrams = dto.GrossWeight;
        entity.CosmosNetWeightGrams = dto.NetWeight;
        entity.CosmosWidth = dto.Width;
        entity.CosmosHeight = dto.Height;
        entity.CosmosLength = dto.Length;
    }

    private static void ClearCosmosRealSkuColumns(Product entity)
    {
        entity.CosmosCommercialDescription = null;
        entity.CosmosGtin = null;
        entity.CosmosThumbnailUrl = null;
        entity.CosmosBrandName = null;
        entity.CosmosBrandPictureUrl = null;
        entity.CosmosAvgPrice = null;
        entity.CosmosMaxPrice = null;
        entity.CosmosMinPrice = null;
        entity.CosmosPriceLabel = null;
        entity.CosmosNcmCode = null;
        entity.CosmosNcmDescription = null;
        entity.CosmosGpcCode = null;
        entity.CosmosGpcDescription = null;
        entity.CosmosGrossWeightGrams = null;
        entity.CosmosNetWeightGrams = null;
        entity.CosmosWidth = null;
        entity.CosmosHeight = null;
        entity.CosmosLength = null;
    }

    private static RealSkuFromCosmos? BuildRealSku(Product p)
    {
        if (HasPersistedCosmosColumns(p))
            return RealSkuFromColumns(p);

        if (string.IsNullOrWhiteSpace(p.CosmosMetadataJson))
            return null;

        try
        {
            var dto = JsonSerializer.Deserialize<CosmosGtinProductDto>(p.CosmosMetadataJson, ReadStoredCosmosOptions);
            return dto is null ? null : ToRealSku(dto, p.Sku);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasPersistedCosmosColumns(Product p) =>
        p.CosmosCommercialDescription != null
        || p.CosmosGtin != null
        || p.CosmosThumbnailUrl != null
        || p.CosmosBrandName != null
        || p.CosmosBrandPictureUrl != null
        || p.CosmosAvgPrice != null
        || p.CosmosMaxPrice != null
        || p.CosmosMinPrice != null
        || p.CosmosPriceLabel != null
        || p.CosmosNcmCode != null
        || p.CosmosNcmDescription != null
        || p.CosmosGpcCode != null
        || p.CosmosGpcDescription != null
        || p.CosmosGrossWeightGrams != null
        || p.CosmosNetWeightGrams != null
        || p.CosmosWidth != null
        || p.CosmosHeight != null
        || p.CosmosLength != null;

    private static RealSkuFromCosmos RealSkuFromColumns(Product p)
    {
        var gtinDigits = string.Concat(p.Sku.Where(char.IsAsciiDigit));
        var gtin = p.CosmosGtin ?? (gtinDigits.Length is >= 8 and <= 14 ? gtinDigits : null);
        return new RealSkuFromCosmos(
            p.CosmosCommercialDescription,
            gtin,
            p.CosmosThumbnailUrl,
            p.CosmosBrandName,
            p.CosmosBrandPictureUrl,
            p.CosmosAvgPrice,
            p.CosmosMaxPrice,
            p.CosmosMinPrice,
            p.CosmosPriceLabel,
            p.CosmosNcmCode,
            p.CosmosNcmDescription,
            p.CosmosGpcCode,
            p.CosmosGpcDescription,
            p.CosmosGrossWeightGrams,
            p.CosmosNetWeightGrams,
            p.CosmosWidth,
            p.CosmosHeight,
            p.CosmosLength);
    }

    private static RealSkuFromCosmos ToRealSku(CosmosGtinProductDto dto, string skuFallback)
    {
        var digits = string.Concat(skuFallback.Where(char.IsAsciiDigit));
        var gtin = dto.Gtin?.ToString();
        if (string.IsNullOrEmpty(gtin) && digits.Length is >= 8 and <= 14)
            gtin = digits;

        return new RealSkuFromCosmos(
            dto.Description,
            gtin,
            dto.Thumbnail,
            dto.Brand?.Name,
            dto.Brand?.Picture,
            dto.AvgPrice,
            dto.MaxPrice,
            dto.MinPrice,
            dto.PriceLabel,
            dto.Ncm?.Code,
            !string.IsNullOrWhiteSpace(dto.Ncm?.Description) ? dto.Ncm.Description : dto.Ncm?.FullDescription,
            dto.Gpc?.Code,
            dto.Gpc?.Description,
            dto.GrossWeight,
            dto.NetWeight,
            dto.Width,
            dto.Height,
            dto.Length);
    }
}
