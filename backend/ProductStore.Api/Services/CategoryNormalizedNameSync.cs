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
        var categories = db.Categories.ToList();
        var any = false;
        foreach (var c in categories)
        {
            var n = CategoryRules.NormalizeCategoryName(c.Name);
            if (c.NormalizedName != n)
            {
                c.NormalizedName = n;
                any = true;
            }
        }

        if (any)
            db.SaveChanges();
    }
}
