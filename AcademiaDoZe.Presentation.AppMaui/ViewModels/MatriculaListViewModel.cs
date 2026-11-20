using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : BaseViewModel
{
    public ObservableCollection<string> FilterTypes { get; } = ["Id", "Id Aluno", "Ativas", "Vencendo em dias"];

    private readonly IMatriculaService _matriculaService;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Ativas";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    private ObservableCollection<MatriculaDto> _matriculas = [];
    public ObservableCollection<MatriculaDto> Matriculas
    {
        get => _matriculas;
        set => SetProperty(ref _matriculas, value);
    }

    private MatriculaDto? _selectedMatricula;
    public MatriculaDto? SelectedMatricula
    {
        get => _selectedMatricula;
        set => SetProperty(ref _selectedMatricula, value);
    }

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = L.T("strMatricula");
    }

    [RelayCommand]
    private async Task AddMatriculaAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("matricula");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroNavegarCadastro", ex.Message), "OK");
        }
    }

    [RelayCommand]
    private async Task EditMatriculaAsync(MatriculaDto matricula)
    {
        try
        {
            if (matricula == null)
                return;

            await Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");
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
        await LoadMatriculasAsync();
    }

    [RelayCommand]
    private async Task SearchMatriculasAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Limpa a lista atual
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
            });

            IEnumerable<MatriculaDto> resultados = [];

            // Busca as matrículas de acordo com o filtro
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                if (SelectedFilterType == "Ativas")
                {
                    resultados = await _matriculaService.ObterAtivasAsync(0, cts.Token) ?? [];
                }
                else
                {
                    resultados = await _matriculaService.ObterTodasAsync(cts.Token) ?? [];
                }
            }
            else if (SelectedFilterType == "Id")
            {
                if (int.TryParse(SearchText.Trim(), out int id))
                {
                    var matricula = await _matriculaService.ObterPorIdAsync(id, cts.Token);
                    if (matricula != null)
                        resultados = [matricula];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgBuscaIdValido"), "OK");
                }
            }
            else if (SelectedFilterType == "Id Aluno")
            {
                if (int.TryParse(SearchText.Trim(), out int alunoId))
                {
                    resultados = await _matriculaService.ObterPorAlunoIdAsync(alunoId, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgBuscaIdAlunoValido"), "OK");
                }
            }
            else if (SelectedFilterType == "Ativas")
            {
                int.TryParse(SearchText.Trim(), out int alunoId);
                resultados = await _matriculaService.ObterAtivasAsync(alunoId, cts.Token) ?? [];
            }
            else if (SelectedFilterType == "Vencendo em dias")
            {
                if (int.TryParse(SearchText.Trim(), out int dias))
                {
                    resultados = await _matriculaService.ObterVencendoEmDiasAsync(dias, cts.Token) ?? [];
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgBuscaVencimentoDias"), "OK");
                }
            }

            // Atualiza a coleção na thread principal
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var item in resultados)
                {
                    Matriculas.Add(item);
                }
                OnPropertyChanged(nameof(Matriculas));
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroBuscarMatriculas", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMatriculasAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Limpa a lista atual antes de carregar novos dados
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                OnPropertyChanged(nameof(Matriculas));
            });

            var matriculasList = await _matriculaService.ObterTodasAsync(cts.Token);

            if (matriculasList != null)
            {
                // Garantir que a atualização da UI aconteça na thread principal
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var matricula in matriculasList)
                    {
                        Matriculas.Add(matricula);
                    }

                    OnPropertyChanged(nameof(Matriculas));
                });
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroCarregarMatriculas", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto matricula)
    {
        if (matricula == null)
            return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            L.T("strConfirmarExclusao"),
            L.F("msgConfirmaExcluirMatricula", matricula.Id),
            L.T("strSim"), L.T("strNao"));

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool success = await _matriculaService.RemoverAsync(matricula.Id, cts.Token);

            if (success)
            {
                Matriculas.Remove(matricula);
                await Shell.Current.DisplayAlertAsync(L.T("strSucesso"), L.T("msgMatriculaExcluida"), "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.T("msgMatriculaNaoExcluida"), "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroExcluirMatricula", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
