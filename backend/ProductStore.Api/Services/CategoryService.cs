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

        var normalized = CategoryRules.NormalizeCategoryName(name);

        var exists = await db.Categories.AsNoTracking()

            .AnyAsync(c => c.NormalizedName == normalized, cancellationToken);

        if (exists)

            throw new DuplicateCategoryNameException(name);



        var entity = new Category { Id = Guid.NewGuid(), Name = name, NormalizedName = normalized };

        db.Categories.Add(entity);

        try

        {

            await db.SaveChangesAsync(cancellationToken);

        }

        catch (DbUpdateException)

        {

            throw new DuplicateCategoryNameException(name);

        }

        logger.LogInformation("Categoria criada Id={CategoryId} Name={Name}", entity.Id, entity.Name);

        return new CategoryResponse(entity.Id, entity.Name);

    }



    public async Task<IReadOnlyList<CategoryFieldResponse>> ListFieldsAsync(Guid categoryId, CancellationToken cancellationToken = default)

    {

        var exists = await db.Categories.AsNoTracking()

            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!exists)

            throw new CategoryNotFoundException(categoryId);



        return await db.CategoryFieldDefinitions.AsNoTracking()

            .Where(f => f.CategoryId == categoryId)

            .OrderBy(f => f.SortOrder)

            .ThenBy(f => f.Name)

            .Select(f => new CategoryFieldResponse(f.Id, f.CategoryId, f.Name, f.SortOrder))

            .ToListAsync(cancellationToken);

    }



    public async Task<CategoryFieldResponse> AddFieldAsync(

        Guid categoryId,

        CreateCategoryFieldRequest request,

        CancellationToken cancellationToken = default)

    {

        _ = await db.Categories.AsNoTracking()

            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)

            ?? throw new CategoryNotFoundException(categoryId);



        var name = request.Name.Trim();

        if (string.IsNullOrEmpty(name))

            throw new ArgumentException("Nome do campo é obrigatório.", nameof(request));



        var normalized = CategoryRules.NormalizeCategoryName(name);

        var duplicate = await db.CategoryFieldDefinitions.AsNoTracking()

            .AnyAsync(f => f.CategoryId == categoryId && f.NormalizedName == normalized, cancellationToken);

        if (duplicate)

            throw new DuplicateCategoryFieldNameException(name);



        var maxOrder = await db.CategoryFieldDefinitions

            .Where(f => f.CategoryId == categoryId)

            .Select(f => (int?)f.SortOrder)

            .MaxAsync(cancellationToken) ?? 0;



        var entity = new CategoryFieldDefinition

        {

            Id = Guid.NewGuid(),

            CategoryId = categoryId,

            Name = name,

            NormalizedName = normalized,

            SortOrder = maxOrder + 1,

        };

        db.CategoryFieldDefinitions.Add(entity);

        try

        {

            await db.SaveChangesAsync(cancellationToken);

        }

        catch (DbUpdateException)

        {

            throw new DuplicateCategoryFieldNameException(name);

        }



        logger.LogInformation(

            "Campo de categoria criado Id={FieldId} CategoryId={CategoryId} Name={Name}",

            entity.Id,

            categoryId,

            entity.Name);



        return new CategoryFieldResponse(entity.Id, entity.CategoryId, entity.Name, entity.SortOrder);

    }



    public async Task DeleteFieldAsync(Guid categoryId, Guid fieldId, CancellationToken cancellationToken = default)

    {

        var field = await db.CategoryFieldDefinitions

            .FirstOrDefaultAsync(f => f.Id == fieldId && f.CategoryId == categoryId, cancellationToken)

            ?? throw new CategoryFieldNotFoundException(categoryId, fieldId);



        db.CategoryFieldDefinitions.Remove(field);

        await db.SaveChangesAsync(cancellationToken);



        logger.LogInformation("Campo de categoria removido FieldId={FieldId} CategoryId={CategoryId}", fieldId, categoryId);

    }

}

