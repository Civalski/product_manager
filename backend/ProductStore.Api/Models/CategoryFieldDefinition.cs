namespace ProductStore.Api.Models;

public class CategoryFieldDefinition
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
