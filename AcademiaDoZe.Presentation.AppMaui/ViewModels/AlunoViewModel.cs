using AcademiaDoZe.Presentation.AppMaui.Helpers;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(AlunoId), "Id")]
public partial class AlunoViewModel : BaseViewModel
{
    private readonly IAlunoService _alunoService;
    private readonly ILogradouroService _logradouroService;

    private AlunoDto _aluno = CriarAlunoPadrao();
    public AlunoDto Aluno
    {
        get => _aluno;
        set => SetProperty(ref _aluno, value);
    }

    private int _alunoId;
    public int AlunoId
    {
        get => _alunoId;
        set => SetProperty(ref _alunoId, value);
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

    public bool HasEnderecoVinculado => Aluno?.Endereco != null && Aluno.Endereco.Id > 0;
    public bool HasNoEnderecoVinculado => !HasEnderecoVinculado;

    public AlunoViewModel(IAlunoService alunoService, ILogradouroService logradouroService)
    {
        _alunoService = alunoService;
        _logradouroService = logradouroService;
        Title = L.T("strDetalhesAluno");
    }

    public async Task InitializeAsync()
    {
        ConfirmarSenha = string.Empty;
        if (AlunoId > 0)
        {
            IsEditMode = true;
            Title = L.T("strEditarAluno");
            await LoadAlunoAsync();
        }
        else
        {
            IsEditMode = false;
            Title = L.T("strNovoAluno");
            Aluno = CriarAlunoPadrao();
            NotificarStatusEndereco();
        }
    }

    private void NotificarStatusEndereco()
    {
        OnPropertyChanged(nameof(HasEnderecoVinculado));
        OnPropertyChanged(nameof(HasNoEnderecoVinculado));
    }

    private static AlunoDto CriarAlunoPadrao() => new()
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
        Complemento = string.Empty
    };

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task LoadAlunoAsync()
    {
        if (AlunoId <= 0)
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var alunoData = await _alunoService.ObterPorIdAsync(AlunoId, cts.Token);
            if (alunoData != null)
            {
                // Garante que o Endereço não seja nulo para evitar erros de bind
                alunoData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };
                // A senha não é retornada por motivos de segurança; limpamos para preenchimento opcional
                alunoData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Aluno = alunoData;
                NotificarStatusEndereco();
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgCargaAlunoExpirou"), "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroCarregarAluno", ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCpfAsync()
    {
        if (string.IsNullOrWhiteSpace(Aluno.Cpf))
        {
            await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("strInformeCpfBusca"), "OK");
            return;
        }
        var apenasDigitos = new string([.. Aluno.Cpf.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strValidacao"), L.T("msgCpf11"), "OK");
            return;
        }
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var resumo = await _alunoService.ObterPorCpfAsync(apenasDigitos, cts.Token);
            // Recarrega por Id para trazer o logradouro completo (a busca por CPF não preenche o endereço)
            var alunoData = resumo != null
                ? await _alunoService.ObterPorIdAsync(resumo.Id, cts.Token) ?? resumo
                : null;
            if (alunoData != null)
            {
                alunoData.Endereco ??= new LogradouroDto
                {
                    Cep = string.Empty,
                    Nome = string.Empty,
                    Bairro = string.Empty,
                    Cidade = string.Empty,
                    Estado = string.Empty,
                    Pais = "Brasil"
                };
                alunoData.Senha = string.Empty;
                ConfirmarSenha = string.Empty;
                Aluno = alunoData;
                AlunoId = alunoData.Id;
                IsEditMode = true;
                Title = L.T("strEditarAluno");
                NotificarStatusEndereco();
                await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgAlunoJaCadastrado"), "OK");
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
        if (Aluno.Endereco == null || string.IsNullOrWhiteSpace(Aluno.Endereco.Cep))
        {
            await Shell.Current.DisplayAlertAsync(L.T("strAviso"), L.T("msgInformeCep"), "OK");
            return;
        }
        var apenasDigitos = new string([.. Aluno.Endereco.Cep.Where(char.IsDigit)]);
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
                Aluno.Endereco = logradouroData;
                OnPropertyChanged(nameof(Aluno));
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
    public async Task SaveAlunoAsync()
    {
        if (IsBusy)
            return;
        if (!await ValidateAlunoAsync(Aluno, IsEditMode, ConfirmarSenha))
            return;
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            // Se o endereço foi digitado mas não tem ID ainda, tenta buscar pelo CEP
            if (Aluno.Endereco != null && Aluno.Endereco.Id <= 0 && !string.IsNullOrWhiteSpace(Aluno.Endereco.Cep))
            {
                var cepLimpo = new string([.. Aluno.Endereco.Cep.Where(char.IsDigit)]);
                if (cepLimpo.Length == 8)
                {
                    var logradouroData = await _logradouroService.ObterPorCepAsync(cepLimpo, cts.Token);
                    if (logradouroData != null)
                    {
                        Aluno.Endereco = logradouroData;
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
            Aluno.Nome = Aluno.Nome.Trim();
            Aluno.Cpf = new string([.. Aluno.Cpf.Where(char.IsDigit)]);
            Aluno.Telefone = new string([.. Aluno.Telefone.Where(char.IsDigit)]);
            Aluno.Numero = Aluno.Numero.Trim();
            Aluno.Complemento = string.IsNullOrWhiteSpace(Aluno.Complemento) ? null : Aluno.Complemento.Trim();
            Aluno.Email = string.IsNullOrWhiteSpace(Aluno.Email) ? null : Aluno.Email.Trim();
            Aluno.Senha = string.IsNullOrWhiteSpace(Aluno.Senha) ? null : Aluno.Senha.Trim();
            if (IsEditMode)
            {
                await _alunoService.AtualizarAsync(Aluno, cts.Token);
                await Shell.Current.DisplayAlertAsync(L.T("strSucesso"), L.T("msgAlunoAtualizado"), "OK");
            }
            else
            {
                await _alunoService.AdicionarAsync(Aluno, cts.Token);
                await Shell.Current.DisplayAlertAsync(L.T("strSucesso"), L.T("msgAlunoCriado"), "OK");
            }
            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(L.T("strTempoEsgotado"), L.T("msgGravacaoAlunoExpirou"), "OK");
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
            await Shell.Current.DisplayAlertAsync(L.T("strErro"), L.F("msgErroSalvarAluno", ex.Message), "OK");
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
                Aluno.Foto = null;
                OnPropertyChanged(nameof(Aluno));
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
                Aluno.Foto = new ArquivoDto { Conteudo = ms.ToArray() };
                OnPropertyChanged(nameof(Aluno));
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
    private static async Task<bool> ValidateAlunoAsync(AlunoDto aluno, bool isEditMode, string confirmarSenha)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(aluno.Nome))
        {
            errors.Add(L.T("valNome"));
        }
        if (string.IsNullOrWhiteSpace(aluno.Cpf))
        {
            errors.Add(L.T("valCpf"));
        }
        else
        {
            var apenasDigitosCpf = new string([.. aluno.Cpf.Where(char.IsDigit)]);
            if (apenasDigitosCpf.Length != 11)
            {
                errors.Add(L.T("valCpf11"));
            }
        }
        if (aluno.DataNascimento == default)
        {
            errors.Add(L.T("valNascimento"));
        }
        else
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            if (aluno.DataNascimento > hoje)
            {
                errors.Add(L.T("valNascimentoFutura"));
            }
            else if (aluno.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-12)))
            {
                errors.Add(L.T("valAluno12"));
            }
        }
        if (string.IsNullOrWhiteSpace(aluno.Telefone))
        {
            errors.Add(L.T("valTelefone"));
        }
        else
        {
            var apenasDigitosTel = new string([.. aluno.Telefone.Where(char.IsDigit)]);
            if (apenasDigitosTel.Length != 11)
            {
                errors.Add(L.T("valTelefone11"));
            }
        }
        if (!string.IsNullOrWhiteSpace(aluno.Email))
        {
            var emailLimpo = aluno.Email.Trim();
            if (!ValidarFormatoEmail(emailLimpo))
            {
                errors.Add(L.T("valEmail"));
            }
        }
        // Senha: obrigatória no cadastro; na edição só valida se foi preenchida
        if (!isEditMode)
        {
            if (string.IsNullOrWhiteSpace(aluno.Senha))
            {
                errors.Add(L.T("valSenha"));
            }
            else
            {
                var senhaLimpa = aluno.Senha.Trim();
                if (senhaLimpa.Length < 6 || !senhaLimpa.Any(char.IsUpper))
                {
                    errors.Add(L.T("valSenha6"));
                }
                if (aluno.Senha != confirmarSenha)
                {
                    errors.Add(L.T("valSenhaConfere"));
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(aluno.Senha))
        {
            var senhaLimpa = aluno.Senha.Trim();
            if (senhaLimpa.Length < 6 || !senhaLimpa.Any(char.IsUpper))
            {
                errors.Add(L.T("valNovaSenha6"));
            }
            if (aluno.Senha != confirmarSenha)
            {
                errors.Add(L.T("valNovaSenhaConfere"));
            }
        }
        if (aluno.Endereco == null || string.IsNullOrWhiteSpace(aluno.Endereco.Cep))
        {
            errors.Add(L.T("valCepEndereco"));
        }
        if (string.IsNullOrWhiteSpace(aluno.Numero))
        {
            errors.Add(L.T("valNumeroEndereco"));
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
