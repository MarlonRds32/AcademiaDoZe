using AcademiaDoZe.Presentation.AppMaui.Helpers;
using System.Globalization;
using System.Text;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Traduz o nome do filtro exibido no Picker das listagens ("Nome", "Id Aluno", "Vínculo"...).
// O valor interno continua o mesmo; só o texto exibido muda. Chave no .resx: flt + nome sem espaços e sem acentos.
public sealed class FiltroDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string nome || string.IsNullOrWhiteSpace(nome)) return string.Empty;

        var sb = new StringBuilder("flt");
        foreach (var c in nome.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        var key = sb.ToString();
        var texto = L.T(key);
        return texto == key ? nome : texto;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
