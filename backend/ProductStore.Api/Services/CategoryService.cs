using Microsoft.EntityFrameworkCore;

using ProductStore.Api.Data;

using ProductStore.Api.Domain;

using ProductStore.Api.DTOs;

using ProductStore.Api.Exceptions;

using ProductStore.Api.Models;



namespace ProductStore.Api.Services;



public class CategoryService(AppDbContext db, ILogger<CategoryService> logger) : ICategoryService

{

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken = default)

    {

        return await db.Categories.AsNoTracking()

            .OrderBy(c => c.Name)

            .Select(c => new CategoryResponse(c.Id, c.Name))

            .ToListAsync(cancellationToken);

    }



    public async Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)

    {

        return await db.Categories.AsNoTracking()

            .Where(c => c.Id == id)

            .Select(c => new CategoryResponse(c.Id, c.Name))

            .FirstOrDefaultAsync(cancellationToken);

    }



    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)

    {

        var name = request.Name.Trim();

        if (string.IsNullOrEmpty(name))

            throw new ArgumentException("Nome da categoria é obrigatório.", nameof(request));



        var existingNames = await db.Categories.AsNoTracking()

            .Select(c => c.Name)

            .ToListAsync(cancellationToken);

        if (existingNames.Any(n => CategoryRules.AreEquivalent(n, name)))

            throw new DuplicateCategoryNameException(name);



        var entity = new Category { Id = Guid.NewGuid(), Name = name };

        db.Categories.Add(entity);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Categoria criada Id={CategoryId} Name={Name}", entity.Id, entity.Name);

        return new CategoryResponse(entity.Id, entity.Name);

    }

}

