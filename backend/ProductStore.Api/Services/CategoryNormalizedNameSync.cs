using Microsoft.EntityFrameworkCore;
using ProductStore.Api.Data;
using ProductStore.Api.Domain;

namespace ProductStore.Api.Services;

/// <summary>
/// Alinha <see cref="Category.NormalizedName"/> com <see cref="CategoryRules.NormalizeCategoryName"/> (acentos, etc.).
/// O SQL da migração SQLite só aplica LOWER(TRIM); esta sincronização corrige linhas existentes após migrar.
/// </summary>
public static class CategoryNormalizedNameSync
{
    public static void AfterMigrate(AppDbContext db)
    {
        var categories = db.Categories
            .ToList();

        var collisions = categories
            .Select(c => new CategoryNormalizationCandidate(
                c,
                CategoryRules.NormalizeCategoryName(c.Name)))
            .GroupBy(c => c.ComputedNormalizedName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(c => $"\"{c.Category.Name}\""))}")
            .ToList();

        if (collisions.Count > 0)
        {
            throw new InvalidOperationException(
                "Conflito ao normalizar categorias existentes. " +
                "Os nomes abaixo passam a ser iguais após remover acentos e normalizar espaços/letras: " +
                string.Join("; ", collisions));
        }

        var any = false;
        foreach (var category in categories)
        {
            var normalizedName = CategoryRules.NormalizeCategoryName(category.Name);
            if (category.NormalizedName != normalizedName)
            {
                category.NormalizedName = normalizedName;
                any = true;
            }
        }

        if (any)
            db.SaveChanges();
    }

    private sealed record CategoryNormalizationCandidate(
        Models.Category Category,
        string ComputedNormalizedName);
}
