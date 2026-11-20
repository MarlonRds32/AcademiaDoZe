namespace AcademiaDoZe.Presentation.AppMaui.Helpers;

/// <summary>
/// Extensão de marcação XAML para internacionalização simplificada.
/// Permite usar no XAML: Text="{helpers:Translate strTitulo}" ou Format="{helpers:Translate FormatoDataLonga}"
/// sem necessitar desativar a compilação com x:DataType="{x:Null}".
/// Por convenção do XAML/MAUI o sufixo Extension é omitido. Exemplo: {helpers:Translate ...}
/// </summary>
[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var path = Key switch
        {
            nameof(LocalizationManager.FormatoDataCurta) => nameof(LocalizationManager.FormatoDataCurta),
            nameof(LocalizationManager.FormatoDataLonga) => nameof(LocalizationManager.FormatoDataLonga),
            _ => $"[{Key}]"
        };

        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = path,
            Source = LocalizationManager.Instance
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
