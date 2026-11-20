using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class ColaboradorListViewModel : BaseViewModel
{
    private readonly IColaboradorService _colaboradorService;

    public ObservableCollection<string> FilterTypes { get; } = ["Nome", "CPF", "Id", "Email", "Tipo", "Vínculo"];

    private ObservableCollection<ColaboradorDto> _colaboradores = [];
    public ObservableCollection<ColaboradorDto> Colaboradores
    {
        get => _colaboradores;
        set => SetProperty(ref _colaboradores, value);
    }

    private ColaboradorDto? _selectedColaborador;
    public ColaboradorDto? SelectedColaborador
    {
        get => _selectedColaborador;
        set => SetProperty(ref _selectedColaborador, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Nome";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public ColaboradorListViewModel(IColaboradorService colaboradorService)
    {
        _colaboradorService = colaboradorService;
        Title = L.T("strColaborador");
    }

    [RelayCommand]
    private async Task AddColaboradorAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("colaborador");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroNavegarCadastro", ex.Message), "OK");
        }
    }

    [RelayCommand]
    private async Task EditColaboradorAsync(ColaboradorDto colaborador)
    {
        try
        {
            if (colaborador == null)
                return;
            await Shell.Current.GoToAsync($"colaborador?Id={colaborador.Id}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroNavegarEdicao", ex.Message), "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadColaboradoresAsync();
    }

    [RelayCommand]
    private async Task SearchColaboradoresAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            // Limpa a lista atual
            await MainThread.InvokeOnMainThreadAsync(() => { Colaboradores.Clear(); });
            IEnumerable<ColaboradorDto> resultados = [];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Busca os colaboradores de acordo com o filtro selecionado
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                resultados = await _colaboradorService.ObterTodosAsync(cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Nome")
            {
                var todos = await _colaboradorService.ObterTodosAsync(cts.Token) ?? [];
                var termo = SearchText.Trim();
                resultados = todos.Where(c => c.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }
            else if (SelectedFilterType == "CPF")
            {
                var cpfLimpo = new string([.. SearchText.Where(char.IsDigit)]);
                if (cpfLimpo.Length != 11)
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgBuscaCpf11"), "OK");
                    return;
                }
                var colaborador = await _colaboradorService.ObterPorCpfAsync(cpfLimpo, cts.Token);
                if (colaborador != null)
                    resultados = [colaborador];
            }
            else if (SelectedFilterType == "Id")
            {
                if (!int.TryParse(SearchText.Trim(), out int id) || id <= 0)
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgBuscaIdInteiro"), "OK");
                    return;
                }
                var colaborador = await _colaboradorService.ObterPorIdAsync(id, cts.Token);
                if (colaborador != null)
                    resultados = [colaborador];
            }
            else if (SelectedFilterType == "Email")
            {
                var emailBusca = SearchText.Trim();
                var colaborador = await _colaboradorService.ObterPorEmailAsync(emailBusca, cts.Token);
                if (colaborador != null)
                    resultados = [colaborador];
            }
            else if (SelectedFilterType == "Tipo")
            {
                var termo = SearchText.Trim();
                var correspondentes = Enum.GetValues<AppColaboradorTipo>()
                    .Where(t => t.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase) || t.GetDisplayName().Contains(termo, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (correspondentes.Count > 0)
                {
                    var listaPorTipo = new List<ColaboradorDto>();
                    foreach (var t in correspondentes)
                    {
                        var itens = await _colaboradorService.ObterPorTipoAsync(t, cts.Token);
                        if (itens != null) listaPorTipo.AddRange(itens);
                    }
                    resultados = listaPorTipo.DistinctBy(c => c.Id);
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgFiltroTipoInvalido"), "OK");
                    return;
                }
            }
            else if (SelectedFilterType == "Vínculo")
            {
                var termo = SearchText.Trim();
                var correspondentes = Enum.GetValues<AppColaboradorVinculo>()
                    .Where(v => v.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase) || v.GetDisplayName().Contains(termo, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (correspondentes.Count > 0)
                {
                    var listaPorVinculo = new List<ColaboradorDto>();
                    foreach (var v in correspondentes)
                    {
                        var itens = await _colaboradorService.ObterPorVinculoAsync(v, cts.Token);
                        if (itens != null) listaPorVinculo.AddRange(itens);
                    }
                    resultados = listaPorVinculo.DistinctBy(c => c.Id);
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgFiltroVinculoInvalido"), "OK");
                    return;
                }
            }
            // Atualiza a coleção na thread principal
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var item in resultados)
                {
                    Colaboradores.Add(item);
                }
                OnPropertyChanged(nameof(Colaboradores));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgBuscaColaboradoresExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroBuscarColaboradores", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadColaboradoresAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            // Limpa a lista atual antes de carregar novos dados
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Colaboradores.Clear();
                OnPropertyChanged(nameof(Colaboradores));
            });
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var colaboradoresList = await _colaboradorService.ObterTodosAsync(cts.Token);
            if (colaboradoresList != null)
            {
                // Garantir que a atualização da UI aconteça na thread principal
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var colaborador in colaboradoresList)
                    {
                        Colaboradores.Add(colaborador);
                    }
                    OnPropertyChanged(nameof(Colaboradores));
                });
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgCargaColaboradoresExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroCarregarColaboradores", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteColaboradorAsync(ColaboradorDto colaborador)
    {
        if (colaborador == null)
            return;
        bool confirm = await Shell.Current.DisplayAlertAsync(
            L.T("strConfirmarExclusao"),
            L.F("msgConfirmaExcluirColaborador", colaborador.Nome),
            L.T("strSim"), L.T("strNao"));
        if (!confirm)
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _colaboradorService.RemoverAsync(colaborador.Id, cts.Token);
            if (success)
            {
                Colaboradores.Remove(colaborador);
                await Shell.Current.DisplayAlertAsync(L.T("strSucesso"), L.T("msgColaboradorExcluido"), "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.T("msgColaboradorNaoExcluido"), "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgExclusaoColaboradorExpirou"), "OK");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("REFERENCE", StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.DisplayAlertAsync(L.T("strNaoPermitido"),
                    L.T("msgColaboradorVinculado"), "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroExcluirColaborador", ex.Message), "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
