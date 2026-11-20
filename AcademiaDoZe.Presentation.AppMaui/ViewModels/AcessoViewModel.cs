using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Exceptions;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Helpers;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

// Avaliação 03 - Entrada/Saída de Alunos e Colaboradores
public partial class AcessoViewModel : BaseViewModel
{
    private readonly IAcessoAlunoService _acessoAlunoService;
    private readonly IAcessoColaboradorService _acessoColaboradorService;

    // true = registrando aluno, false = registrando colaborador
    private bool _isAluno = true;
    public bool IsAluno
    {
        get => _isAluno;
        set
        {
            if (SetProperty(ref _isAluno, value))
                OnPropertyChanged(nameof(IsColaborador));
        }
    }
    public bool IsColaborador => !IsAluno;

    private string _cpf = string.Empty;
    public string Cpf
    {
        get => _cpf;
        set => SetProperty(ref _cpf, value);
    }

    // Último registro realizado com sucesso
    private AcessoItem? _resultado;
    public AcessoItem? Resultado
    {
        get => _resultado;
        set
        {
            if (SetProperty(ref _resultado, value))
                OnPropertyChanged(nameof(HasResultado));
        }
    }
    public bool HasResultado => Resultado != null;

    // Mensagem da regra que impediu o registro (sem matrícula ativa, limite de horas...)
    private string _mensagemErro = string.Empty;
    public string MensagemErro
    {
        get => _mensagemErro;
        set
        {
            if (SetProperty(ref _mensagemErro, value))
                OnPropertyChanged(nameof(HasErro));
        }
    }
    public bool HasErro => !string.IsNullOrWhiteSpace(MensagemErro);

    public ObservableCollection<AcessoItem> Registros { get; } = [];

    private bool _hasRegistros;
    public bool HasRegistros
    {
        get => _hasRegistros;
        set
        {
            if (SetProperty(ref _hasRegistros, value))
                OnPropertyChanged(nameof(HasNoRegistros));
        }
    }
    public bool HasNoRegistros => !HasRegistros;

    public AcessoViewModel(IAcessoAlunoService acessoAlunoService, IAcessoColaboradorService acessoColaboradorService)
    {
        _acessoAlunoService = acessoAlunoService;
        _acessoColaboradorService = acessoColaboradorService;
        Title = L.T("strControleAcesso");
    }

    [RelayCommand]
    private void SelecionarAluno()
    {
        IsAluno = true;
    }

    [RelayCommand]
    private void SelecionarColaborador()
    {
        IsAluno = false;
    }

    // hh:mm:ss
    private static string FormatarDuracao(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";

    private static string TipoTexto(AppAcessoTipo tipo) => L.NomeEnum(tipo, tipo.GetDisplayName());

    private static AcessoItem ToItem(AcessoAlunoDto dto)
    {
        string detalhe = string.Empty;
        if (dto.Tipo == AppAcessoTipo.Entrada && dto.PlanoDataFim.HasValue)
        {
            // Na entrada, mostrar quanto tempo ainda tem de plano
            var dataFim = dto.PlanoDataFim.Value.ToDateTime(TimeOnly.MinValue).ToString("d", CultureInfo.CurrentCulture);
            detalhe = L.F("msgAcessoPlanoRestante", dataFim, dto.DiasRestantesPlano ?? 0);
        }
        else if (dto.Tipo == AppAcessoTipo.Saida && dto.Permanencia.HasValue)
        {
            // Na saída, mostrar o tempo que permaneceu na academia
            detalhe = L.F("msgAcessoPermanencia", FormatarDuracao(dto.Permanencia.Value));
        }

        return new AcessoItem
        {
            Nome = dto.Aluno.Nome,
            Cpf = dto.Aluno.Cpf,
            Foto = dto.Aluno.Foto,
            DataHora = dto.DataHora,
            IsEntrada = dto.Tipo == AppAcessoTipo.Entrada,
            PessoaTexto = L.T("strPessoaAluno"),
            TipoTexto = TipoTexto(dto.Tipo),
            Detalhe = detalhe
        };
    }

    private static AcessoItem ToItem(AcessoColaboradorDto dto)
    {
        // Na saída, mostrar o tempo que permaneceu na academia, somando todos os registros do dia
        var chave = dto.Tipo == AppAcessoTipo.Entrada ? "msgAcessoColabEntrada" : "msgAcessoColabSaida";
        return new AcessoItem
        {
            Nome = dto.Colaborador.Nome,
            Cpf = dto.Colaborador.Cpf,
            Foto = dto.Colaborador.Foto,
            DataHora = dto.DataHora,
            IsEntrada = dto.Tipo == AppAcessoTipo.Entrada,
            PessoaTexto = L.T("strPessoaColaborador"),
            TipoTexto = TipoTexto(dto.Tipo),
            Detalhe = L.F(chave, FormatarDuracao(dto.HorasTrabalhadasHoje), (int)dto.LimiteDiario.TotalHours)
        };
    }

    // Traduz a regra de negócio violada usando a chave err{Codigo} do .resx
    private static string MensagemDaRegra(RegraAcessoException ex)
    {
        var chave = $"err{ex.Codigo}";
        var texto = L.T(chave);
        if (texto == chave) return ex.Message;
        return string.Format(CultureInfo.CurrentCulture, texto, ex.Args);
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (IsBusy)
            return;

        var apenasDigitos = new string([.. (Cpf ?? string.Empty).Where(char.IsDigit)]);
        if (apenasDigitos.Length != 11)
        {
            Resultado = null;
            MensagemErro = L.T("errCPF_INVALIDO");
            return;
        }

        try
        {
            IsBusy = true;
            MensagemErro = string.Empty;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            AcessoItem item = IsAluno
                ? ToItem(await _acessoAlunoService.RegistrarAsync(apenasDigitos, cts.Token))
                : ToItem(await _acessoColaboradorService.RegistrarAsync(apenasDigitos, cts.Token));

            Resultado = item;
            Cpf = string.Empty;
        }
        catch (RegraAcessoException ex)
        {
            // regra de negócio: sem matrícula ativa, limite de horas, CPF não encontrado, fora do horário
            Resultado = null;
            MensagemErro = MensagemDaRegra(ex);
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgBuscaExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroRegistrarAcesso", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await LoadRegistrosAsync();
    }

    [RelayCommand]
    private async Task LoadRegistrosAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var alunos = await _acessoAlunoService.ObterAcessosDoDiaAsync(cts.Token);
            var colaboradores = await _acessoColaboradorService.ObterAcessosDoDiaAsync(cts.Token);

            var itens = alunos.Select(a => ToItem(a)).Concat(colaboradores.Select(c => ToItem(c))).OrderByDescending(i => i.DataHora).ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Registros.Clear();
                foreach (var item in itens)
                {
                    Registros.Add(item);
                }
                HasRegistros = Registros.Count > 0;
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgBuscaExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroCarregarAcessos", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
