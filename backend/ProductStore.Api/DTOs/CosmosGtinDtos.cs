using System.Text.Json.Serialization;

using ProductStore.Api.Serialization;



namespace ProductStore.Api.DTOs;



/// <summary>Resposta da API Bluesoft Cosmos (gtins/{gtin}.json), nomes em snake_case.</summary>

public sealed class CosmosGtinProductDto

{

    [JsonPropertyName("avg_price")]

    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]

    public decimal? AvgPrice { get; set; }



    [JsonPropertyName("brand")]

    public CosmosBrandDto? Brand { get; set; }



    [JsonPropertyName("description")]

    public string? Description { get; set; }



    [JsonPropertyName("gpc")]

    public CosmosGpcDto? Gpc { get; set; }



    [JsonPropertyName("gross_weight")]

    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]

    public double? GrossWeight { get; set; }



    [JsonPropertyName("gtin")]

    [JsonConverter(typeof(FlexibleNullableInt64Converter))]

    public long? Gtin { get; set; }



    [JsonPropertyName("height")]

    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]

    public double? Height { get; set; }



    [JsonPropertyName("length")]

    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]

    public double? Length { get; set; }



    [JsonPropertyName("max_price")]

    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]

    public decimal? MaxPrice { get; set; }



    [JsonPropertyName("min_price")]

    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]

    public decimal? MinPrice { get; set; }



    [JsonPropertyName("ncm")]

    public CosmosNcmDto? Ncm { get; set; }



    [JsonPropertyName("net_weight")]

    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]

    public double? NetWeight { get; set; }



    [JsonPropertyName("price")]

    public string? PriceLabel { get; set; }



    [JsonPropertyName("thumbnail")]

    public string? Thumbnail { get; set; }



    [JsonPropertyName("width")]

    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]

    public double? Width { get; set; }

}



public sealed class CosmosBrandDto

{

    [JsonPropertyName("name")]

    public string? Name { get; set; }



    [JsonPropertyName("picture")]

    public string? Picture { get; set; }

}



public sealed class CosmosGpcDto

{

    [JsonPropertyName("code")]

    public string? Code { get; set; }



    [JsonPropertyName("description")]

    public string? Description { get; set; }

}



public sealed class CosmosNcmDto

{

    [JsonPropertyName("code")]

    public string? Code { get; set; }



    [JsonPropertyName("description")]

    public string? Description { get; set; }



    [JsonPropertyName("full_description")]

    public string? FullDescription { get; set; }

}


