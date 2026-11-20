using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using System.Globalization;
namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Converte qualquer Enum para o texto do idioma atual (.resx); sem tradução, usa o Display(Name)
public sealed class EnumDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Enum e ? L.NomeEnum(e, e.GetDisplayName()) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
