using System.Globalization;
using System.Text;

namespace ProductStore.Api.Domain;

public static class CategoryRules
{
    /// <summary>Nome canônico de exibição da categoria que exige preço mínimo.</summary>
    public const string ElectronicsNormalized = "eletrônico";
    public const decimal ElectronicsMinPrice = 50m;

    /// <summary>
    /// Comparação de nomes de categoria: ignora maiúsculas/minúsculas e acentos (ex.: eletronico ≡ eletrônico).
    /// </summary>
    public static string NormalizeCategoryName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;
        var trimmed = name.Trim();
        var formD = trimmed.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public static bool AreEquivalent(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
            return true;
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        return NormalizeCategoryName(a) == NormalizeCategoryName(b);
    }

    public static bool IsElectronics(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;
        return AreEquivalent(category, ElectronicsNormalized);
    }
}
