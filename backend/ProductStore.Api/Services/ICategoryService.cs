using ProductStore.Api.DTOs;

namespace ProductStore.Api.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryFieldResponse>> ListFieldsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryFieldResponse> AddFieldAsync(Guid categoryId, CreateCategoryFieldRequest request, CancellationToken cancellationToken = default);
    Task DeleteFieldAsync(Guid categoryId, Guid fieldId, CancellationToken cancellationToken = default);
}
