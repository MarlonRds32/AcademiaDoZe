using AcademiaDoZe.Presentation.AppMaui.ViewModels;
using AcademiaDoZe.Presentation.AppMaui.Views;
using Microsoft.Extensions.Logging;
using AcademiaDoZe.Presentation.AppMaui.Configuration;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
namespace AcademiaDoZe.Presentation.AppMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Define a cultura inicial antes de construir a UI, com base nas preferências salvas do usuário ou cultura do sistema
        LocalizationManager.Instance.SetCulture(Preferences.Get("Cultura", LocalizationManager.ObterCulturaPadrao()));

        var builder = MauiApp.CreateBuilder();
        builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            // Adicione esta linha para registrar a fonte
            fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
        });
        // Configurar serviços da aplicação e repositórios
        ConfigurationHelper.ConfigureServices(builder.Services);

        // Registrar LocalizationManager como Singleton para injeção quando necessário
        builder.Services.AddSingleton(LocalizationManager.Instance);

#if WINDOWS
        // Garante que o CalendarDatePicker do WinUI utilize a cultura/idioma atual para nomes de dias e meses
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("CultureLanguage", (handler, view) =>
        {
            handler.PlatformView.Language = System.Globalization.CultureInfo.CurrentCulture.Name;
        });
#endif

        // Registrar ViewModels
        builder.Services.AddTransient<DashboardListViewModel>();
        builder.Services.AddTransient<LogradouroListViewModel>();
        builder.Services.AddTransient<LogradouroViewModel>();
        builder.Services.AddTransient<ColaboradorListViewModel>();
        builder.Services.AddTransient<ColaboradorViewModel>();
        builder.Services.AddTransient<AlunoListViewModel>();
        builder.Services.AddTransient<AlunoViewModel>();
        builder.Services.AddTransient<MatriculaListViewModel>();
        builder.Services.AddTransient<MatriculaViewModel>();
        builder.Services.AddTransient<AcessoViewModel>();

        // Registrar Views
        builder.Services.AddTransient<DashboardListPage>();
        builder.Services.AddTransient<LogradouroListPage>();
        builder.Services.AddTransient<LogradouroPage>();
        builder.Services.AddTransient<ConfigPage>();
        builder.Services.AddTransient<ColaboradorListPage>();
        builder.Services.AddTransient<ColaboradorPage>();
        builder.Services.AddTransient<AlunoListPage>();
        builder.Services.AddTransient<AlunoPage>();
        builder.Services.AddTransient<MatriculaListPage>();
        builder.Services.AddTransient<MatriculaPage>();
        builder.Services.AddTransient<AcessoPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
