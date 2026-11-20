using AcademiaDoZe.Presentation.AppMaui.Resources.Strings;
using System.ComponentModel;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Helpers
{
    // Classe que notifica a UI sobre a mudança de idioma
    public class LocalizationManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Instance para acesso global
        public static LocalizationManager Instance { get; } = new LocalizationManager();

        // Define a cultura inicial como a salva em Preferences, ou fallback inteligente baseado no sistema
        public LocalizationManager()
        {
            SetCulture(Preferences.Get("Cultura", ObterCulturaPadrao()));
        }

        // Obtém o idioma padrão a partir do sistema operacional instalado (suporta en, es ou fallback pt-BR)
        public static string ObterCulturaPadrao()
        {
            var uiName = CultureInfo.InstalledUICulture.Name;
            if (uiName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en-US";
            if (uiName.StartsWith("es", StringComparison.OrdinalIgnoreCase))
                return "es-ES";
            return "pt-BR";
        }

        // Indexador, permite acessar as strings traduzidas via Binding. Exemplo: Path=[strTitulo] ou [strTitulo]
        public string this[string text] => AppResources.ResourceManager.GetString(text, CultureInfo.CurrentUICulture) ?? text;

        // propriedade para obter o formato de data curto da cultura atual - utilizado em Format de DatePicker, etc
        public string FormatoDataCurta => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        public string FormatoDataLonga => CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;

        // Método principal para troca de idioma
        public void SetCulture(string strCulture)
        {
            // verificar se strCulture é <> de en-US, es-ES, pt-BR
            if (strCulture != "en-US" && strCulture != "es-ES" && strCulture != "pt-BR")
            {
                strCulture = "pt-BR";
            }

            // Salvar a preferência do usuário para uso na inicialização
            Preferences.Set("Cultura", strCulture);

            // cria o objeto CultureInfo com a cultura desejada
            CultureInfo ci = new(strCulture);

            // altera a cultura da thread atual
            Thread.CurrentThread.CurrentCulture = ci; // afeta a formatação de datas, números, moedas
            Thread.CurrentThread.CurrentUICulture = ci; // afeta a localização de strings

            // Define a cultura padrão para novas threads e tasks assíncronas
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

            // Sincroniza a cultura da classe fortemente tipada gerada pelo .resx
            AppResources.Culture = ci;

#if WINDOWS
            try
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = strCulture;
            }
            catch
            {
                // Ignora em caso de restrição de ambiente/sandbox
            }
#endif

            // Notifica a UI para recarregar as strings e formatos. O argumento 'null' notifica que TODAS as propriedades foram alteradas.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

            // notifica alteração do formato de data curta e longa
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormatoDataCurta)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormatoDataLonga)));
        }
    }
}
