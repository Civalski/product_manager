namespace ProductStore.Api.Domain;



/// <summary>Origem do SKU na criação/edição: código interno sequencial ou GTIN validado na Bluesoft Cosmos.</summary>

public enum SkuSource

{

    /// <summary>Código interno (ex.: 000001); não consulta a API Cosmos.</summary>

    Internal = 0,



    /// <summary>GTIN/EAN; exige token Cosmos e produto existente na API; preenche metadados.</summary>

    CosmosGtin = 1

}


