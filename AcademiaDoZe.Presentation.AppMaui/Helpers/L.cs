using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Helpers;

// Atalho para usar os textos localizados no código C# (ViewModels e code-behind).
// L.T("chave") devolve o texto no idioma atual; L.F("chave", valores) preenche os {0}, {1}...
public static class L
{
    public static string T(string key) => LocalizationManager.Instance[key];

    public static string F(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture, T(key), args);

    // Nome amigável de um enum no idioma atual (chave enum{Tipo}_{Valor}); se não houver tradução, usa o texto recebido
    public static string NomeEnum(System.Enum value, string fallback)
    {
        var key = $"enum{value.GetType().Name}_{value}";
        var texto = T(key);
        return texto == key ? fallback : texto;
    }
}
