namespace ProductStore.Api.Exceptions;



public class ProductNotFoundException(Guid id) : Exception($"Produto não encontrado: {id}")

{

    public Guid ProductId { get; } = id;

}



public class DuplicateSkuException(string sku) : Exception($"SKU já cadastrado: {sku}")

{

    public string Sku { get; } = sku;

}



public class CategoryNotFoundException(Guid id) : Exception($"Categoria não encontrada: {id}")

{

    public Guid CategoryId { get; } = id;

}



public class DuplicateCategoryNameException(string name)

    : Exception($"Já existe uma categoria com o nome \"{name}\".")

{

    public string Name { get; } = name;

}



public class ElectronicsMinPriceException(decimal minPrice)

    : Exception($"Para categoria eletrônico, o preço mínimo é R$ {minPrice:0.00}.")

{

    public decimal MinPrice { get; } = minPrice;

}



public class InvalidGtinSkuException(string sku)

    : Exception($"SKU deve ser um GTIN/EAN numérico de 8 a 14 dígitos (após remover caracteres não numéricos). Valor: {sku}")

{

    public string Sku { get; } = sku;

}



public class CosmosProductNotFoundException(string gtin)

    : Exception($"GTIN não encontrado na base Bluesoft Cosmos: {gtin}")

{

    public string Gtin { get; } = gtin;

}



public class CosmosApiException(string message, int suggestedStatusCode) : Exception(message)

{

    public int SuggestedStatusCode { get; } = suggestedStatusCode;

}



/// <summary>Token Cosmos não configurado ao usar SKU originado da API Bluesoft.</summary>

public sealed class CosmosNotConfiguredException()

    : Exception("Configure a variável Cosmos__Token (ou appsettings) para usar GTIN real com a API Bluesoft Cosmos.")

{

}



/// <summary>Não há produtos na base do utilizador para gerar backup.</summary>

public sealed class NoProductsToExportException()

    : Exception("Não existem produtos para guardar no backup.")

{

}



public class CategoryFieldNotFoundException(Guid categoryId, Guid fieldId)

    : Exception($"Campo da categoria não encontrado: categoria {categoryId}, campo {fieldId}.")

{

    public Guid CategoryId { get; } = categoryId;

    public Guid FieldId { get; } = fieldId;

}



public class DuplicateCategoryFieldNameException(string name)

    : Exception($"Já existe um campo com o nome \"{name}\" nesta categoria.")

{

    public string Name { get; } = name;

}



public class InvalidProductCustomFieldsException(string message) : Exception(message)

{

}

