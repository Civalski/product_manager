namespace ProductStore.Api.Models;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<CategoryFieldDefinition> FieldDefinitions { get; set; } = new List<CategoryFieldDefinition>();
}
