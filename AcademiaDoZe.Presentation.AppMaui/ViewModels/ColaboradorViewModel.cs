using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(ColaboradorId), "Id")]
public partial class ColaboradorViewModel : BaseViewModel
{
    public IEnumerable<AppColaboradorTipo> ColaboradorTipos { get; } = Enum.GetValues<AppColaboradorTipo>().Cast<AppColaboradorTipo>();
    public IEnumerable<AppColaboradorVinculo> ColaboradorVinculos { get; } = Enum.GetValues<AppColaboradorVinculo>().Cast<AppColaboradorVinculo>();

    private readonly IColaboradorService _colaboradorService;
    private readonly ILogradouroService _logradouroService;

    private ColaboradorDto _colaborador = CriarColaboradorPadrao();
    public ColaboradorDto Colaborador
    {
        get => _colaborador;
        set => SetProperty(ref _colaborador, value);
    }

    private int _colaboradorId;
    public int ColaboradorId
    {
        get => _colaboradorId;
        set => SetProperty(ref _colaboradorId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetProperty(ref _confirmarSenha, value);
    }

    public bool HasEnderecoVinculado => Colaborador?.Endereco != null && Colaborador.Endereco.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;

    public ColaboradorViewModel(IColaboradorService colaboradorService, ILogradouroService logradouroService)
    {
        _colaboradorService = colaboradorService;
        _logradouroService = logradouroService;
        Title = L.T("strDetalhesColaborador");
    }

    public async Task InitializeAsync()
    {
        ConfirmarSenha = string.Empty;
        if (ColaboradorId > 0)
        {
            IsEditMode = true;
            Title = L.T("strEditarColaborador");
            await LoadColaboradorAsync();
        }
        else
        {
            IsEditMode = false;
            Title = L.T("strNovoColaborador");
            Colaborador = CriarColaboradorPadrao();
            NotificarStatusEndereco();
        }
    }

    private void NotificarStatusEndereco()
    {
        OnPropertyChanged(nameof(HasEnderecoVinculado));
        OnPropertyChanged(nameof(HasNoEnderecoVinculado));
    }

    private static ColaboradorDto CriarColaboradorPadrao() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Email = string.Empty,
        Senha = string.Empty,
        Endereco = new LogradouroDto
        {
            Cep = string.Empty,
            Nome = string.Empty,
            Bairro = string.Empty,
            Cidade = string.Empty,
            Estado = string.Empty,
            Pais = "Brasil"
        },
        Numero = string.Empty,
        Complemento = string.Empty,
        DataAdmissao = DateOnly.FromDateTime(DateTime.Today),
        Tipo = AppColaboradorTipo.Atendente,
        Vinculo = AppColaboradorVinculo.CLT
    };

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task LoadColaboradorAsync()
    {
        if (ColaboradorId <= 0)
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var colaboradorData = await _colaboradorService.ObterPorIdAsync(ColaboradorId, cts.Token);
            if (colaboradorData != null)
            {
                // Garante que o Endereço não seja nulo para evitar erros de bind
                colaboradorData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };
                // A senha não é retornada por motivos de segurança; limpamos para preenchimento opcional
                colaboradorData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Colaborador = colaboradorData;
                NotificarStatusEndereco();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgCargaColaboradorExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroCarregarColaborador", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCpfAsync()
    {
        if (string.IsNullOrWhiteSpace(Colaborador.Cpf))
        {
            await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("strInformeCpfBusca"), "OK");
            return;
        }
        var apenasDigitos = new string([.. Colaborador.Cpf.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgCpf11"), "OK");
            return;
        }
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var resumo = await _colaboradorService.ObterPorCpfAsync(apenasDigitos, cts.Token);
            // Recarrega por Id para trazer o logradouro completo (a busca por CPF não preenche o endereço)
            var colaboradorData = resumo != null
                ? await _colaboradorService.ObterPorIdAsync(resumo.Id, cts.Token) ?? resumo
                : null;
            if (colaboradorData != null)
            {
                colaboradorData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };
                colaboradorData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Colaborador = colaboradorData;
                ColaboradorId = colaboradorData.Id;
                IsEditMode = true;
                Title = L.T("strEditarColaborador");
                NotificarStatusEndereco();
                await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgColaboradorJaCadastrado"), "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgCpfNaoEncontrado"), "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgBuscaCpfExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroBuscarCpf", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCepAsync()
    {
        if (Colaborador.Endereco == null || string.IsNullOrWhiteSpace(Colaborador.Endereco.Cep))
        {
            await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgInformeCep"), "OK");
            return;
        }
        var apenasDigitos = new string([.. Colaborador.Endereco.Cep.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 8)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgCep8"), "OK");
            return;
        }
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var logradouroData = await _logradouroService.ObterPorCepAsync(apenasDigitos, cts.Token);
            if (logradouroData != null)
            {
                Colaborador.Endereco = logradouroData;
                OnPropertyChanged(nameof(Colaborador));
                NotificarStatusEndereco();
                await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgCepEncontrado"), "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgCepNaoCadastrado"), "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgBuscaCepExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroBuscarCep", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SaveColaboradorAsync()
    {
        if (IsBusy)
            return;
        if (!await ValidateColaboradorAsync(Colaborador, IsEditMode, ConfirmarSenha))
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Se o endereço foi digitado mas não tem ID ainda, tenta buscar pelo CEP
            if (Colaborador.Endereco != null && Colaborador.Endereco.Id <= 0 && !string.IsNullOrWhiteSpace(Colaborador.Endereco.Cep))
            {
                var cepLimpo = new string([.. Colaborador.Endereco.Cep.Where(char.IsDigit)]);
                if (cepLimpo.Length == 8)
                {
                    var logradouroData = await _logradouroService.ObterPorCepAsync(cepLimpo, cts.Token);
                    if (logradouroData != null)
                    {
                        Colaborador.Endereco = logradouroData;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync(L.T("strEnderecoObrigatorio"), L.T("msgCepNaoLogradouro"), "OK");
                        return;
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgCepValido8"), "OK");
                    return;
                }
            }
            // Normaliza os dados antes de persistir
            Colaborador.Nome = Colaborador.Nome.Trim();
            Colaborador.Cpf = new string([.. Colaborador.Cpf.Where(char.IsDigit)]);
            Colaborador.Telefone = new string([.. Colaborador.Telefone.Where(char.IsDigit)]);
            Colaborador.Numero = Colaborador.Numero.Trim();
            Colaborador.Complemento = string.IsNullOrWhiteSpace(Colaborador.Complemento) ? null : Colaborador.Complemento.Trim();
            Colaborador.Email = string.IsNullOrWhiteSpace(Colaborador.Email) ? null : Colaborador.Email.Trim();
            Colaborador.Senha = string.IsNullOrWhiteSpace(Colaborador.Senha) ? null : Colaborador.Senha.Trim();
            if (IsEditMode)
            {
                await _colaboradorService.AtualizarAsync(Colaborador, cts.Token);
                await Shell.Current.DisplayAlertAsync(L.T("strSucesso"), L.T("msgColaboradorAtualizado"), "OK");
            }
            else
            {
                await _colaboradorService.AdicionarAsync(Colaborador, cts.Token);
                await Shell.Current.DisplayAlertAsync(L.T("strSucesso"), L.T("msgColaboradorCriado"), "OK");
            }
            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgGravacaoColaboradorExpirou"), "OK");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strRegraNegocio"), ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroSalvarColaborador", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelecionarFotoAsync()
    {
        try
        {
            string escolha = await Shell.Current.DisplayActionSheetAsync(L.T("strOrigemImagem"), L.T("strCancelar"), L.T("strRemoverFoto"), L.T("strGaleria"), L.T("strCamera"));
            if (escolha == L.T("strRemoverFoto"))
            {
                Colaborador.Foto = null;
                OnPropertyChanged(nameof(Colaborador));
                return;
            }
            FileResult? result = null;
            if (escolha == L.T("strGaleria"))
            {
                result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = L.T("strSelecioneImagem"),
                    FileTypes = FilePickerFileType.Images
                });
            }
            else if (escolha == L.T("strCamera"))
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    result = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.T("msgCapturaNaoSuportada"), "OK");
                    return;
                }
            }
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                Colaborador.Foto = new ArquivoDto { Conteudo = ms.ToArray() };
                OnPropertyChanged(nameof(Colaborador));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroSelecionarImagem", ex.Message), "OK");
        }
    }

    /*
     Validação utilizando o padrão Notification Pattern, retornando uma lista de erros para o usuário, em vez de fail-fast exceptions.
     Isso permite que o usuário veja todos os problemas de uma vez e corrija-os antes de tentar salvar novamente.
    */
    private static async Task<bool> ValidateColaboradorAsync(ColaboradorDto colaborador, bool isEditMode, string confirmarSenha)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(colaborador.Nome))
        {
            errors.Add(L.T("valNome"));
        }
        if (string.IsNullOrWhiteSpace(colaborador.Cpf))
        {
            errors.Add(L.T("valCpf"));
        }
        else
        {
            var apenasDigitosCpf = new string([.. colaborador.Cpf.Where(char.IsDigit)]);
            if (apenasDigitosCpf.Length != 11)
            {
                errors.Add(L.T("valCpf11"));
            }
        }
        if (colaborador.DataNascimento == default)
        {
            errors.Add(L.T("valNascimento"));
        }
        else
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            if (colaborador.DataNascimento > hoje)
            {
                errors.Add(L.T("valNascimentoFutura"));
            }
            else if (colaborador.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-12)))
            {
                errors.Add(L.T("valColaborador12"));
            }
        }
        if (string.IsNullOrWhiteSpace(colaborador.Telefone))
        {
            errors.Add(L.T("valTelefone"));
        }
        else
        {
            var apenasDigitosTel = new string([.. colaborador.Telefone.Where(char.IsDigit)]);
            if (apenasDigitosTel.Length != 11)
            {
                errors.Add(L.T("valTelefone11"));
            }
        }
        if (!string.IsNullOrWhiteSpace(colaborador.Email))
        {
            var emailLimpo = colaborador.Email.Trim();
            if (!ValidarFormatoEmail(emailLimpo))
            {
                errors.Add(L.T("valEmail"));
            }
        }
        // Senha: obrigatória no cadastro; na edição só valida se foi preenchida
        if (!isEditMode)
        {
            if (string.IsNullOrWhiteSpace(colaborador.Senha))
            {
                errors.Add(L.T("valSenha"));
            }
            else
            {
                var senhaLimpa = colaborador.Senha.Trim();
                if (senhaLimpa.Length < 6 || !senhaLimpa.Any(char.IsUpper))
                {
                    errors.Add(L.T("valSenha6"));
                }
                if (colaborador.Senha != confirmarSenha)
                {
                    errors.Add(L.T("valSenhaConfere"));
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(colaborador.Senha))
        {
            var senhaLimpa = colaborador.Senha.Trim();
            if (senhaLimpa.Length < 6 || !senhaLimpa.Any(char.IsUpper))
            {
                errors.Add(L.T("valNovaSenha6"));
            }
            if (colaborador.Senha != confirmarSenha)
            {
                errors.Add(L.T("valNovaSenhaConfere"));
            }
        }
        if (colaborador.Endereco == null || string.IsNullOrWhiteSpace(colaborador.Endereco.Cep))
        {
            errors.Add(L.T("valCepEndereco"));
        }
        if (string.IsNullOrWhiteSpace(colaborador.Numero))
        {
            errors.Add(L.T("valNumeroEndereco"));
        }
        if (colaborador.DataAdmissao == default)
        {
            errors.Add(L.T("valAdmissao"));
        }
        else if (colaborador.DataAdmissao > DateOnly.FromDateTime(DateTime.Today))
        {
            errors.Add(L.T("valAdmissaoFutura"));
        }
        // Regra de Vínculo de Administrador: Administrador precisa ser CLT
        if (colaborador.Tipo == AppColaboradorTipo.Administrador && colaborador.Vinculo != AppColaboradorVinculo.CLT)
        {
            errors.Add(L.T("valAdminClt"));
        }
        if (errors.Count > 0)
        {
            var mensagem = L.T("msgCorrijaCampos") + "\n\n" + string.Join("\n", errors);
            await Shell.Current.DisplayAlertAsync(L.T("strErrosValidacao"), mensagem, "OK");
            return false;
        }
        return true;
    }

    private static bool ValidarFormatoEmail(string email)
    {
        var partes = email.Split('@');
        if (partes.Length != 2) return false;
        if (string.IsNullOrWhiteSpace(partes[0])) return false;
        var dominio = partes[1];
        if (string.IsNullOrWhiteSpace(dominio)) return false;
        if (dominio.StartsWith('.') || dominio.EndsWith('.')) return false;
        var labels = dominio.Split('.');
        if (labels.Length < 2) return false;
        if (labels.Any(string.IsNullOrWhiteSpace)) return false;
        return true;
    }
}
