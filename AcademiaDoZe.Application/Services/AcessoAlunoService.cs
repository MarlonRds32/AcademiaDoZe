using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Exceptions;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
namespace AcademiaDoZe.Application.Services;
/*Marlon Rodrigues*/
public class AcessoAlunoService : IAcessoAlunoService
{
    private readonly Func<IAcessoAlunoRepository> _acessoRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;

    public AcessoAlunoService(Func<IAcessoAlunoRepository> acessoRepoFactory, Func<IAlunoRepository> alunoRepoFactory, Func<IMatriculaRepository> matriculaRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _alunoRepoFactory = alunoRepoFactory ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
    }

    public async Task<AcessoAlunoDto> RegistrarAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var cpfResult = Cpf.Criar(new string([.. (cpf ?? string.Empty).Where(char.IsDigit)]));
        if (cpfResult.IsFailure)
            throw new RegraAcessoException("CPF_INVALIDO", "CPF inválido.");

        var aluno = await _alunoRepoFactory().ObterPorCpf(cpfResult.Value!, cancellationToken)
            ?? throw new RegraAcessoException("ALUNO_NAO_ENCONTRADO", "Aluno não encontrado para o CPF informado.");

        var agora = DateTime.Now;
        var hoje = DateOnly.FromDateTime(agora);
        var acessoRepo = _acessoRepoFactory();
        bool estaNaAcademia = await acessoRepo.EstaNaAcademia(aluno.Id, cancellationToken);

        var acessoResult = AcessoAluno.Criar(0, aluno, agora);
        if (acessoResult.IsFailure)
        {
            if (acessoResult.Notifications.Any(n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO"))
                throw new RegraAcessoException("FORA_HORARIO_FUNCIONAMENTO", "A academia funciona das 06:00 às 22:00.");
            throw new RegraAcessoException("ACESSO_INVALIDO", string.Join(", ", acessoResult.Notifications.Select(n => n.Mensagem)));
        }

        if (!estaNaAcademia)
        {
            // ENTRADA: valida se possui matrícula ativa e informa quanto tempo ainda tem de plano
            var matricula = await _matriculaRepoFactory().ObterMatriculaAtivaPorAluno(aluno.Id, cancellationToken)
                ?? throw new RegraAcessoException("ALUNO_SEM_MATRICULA_ATIVA", $"O aluno {aluno.Nome} não possui matrícula ativa.", aluno.Nome);

            var entrada = await acessoRepo.Adicionar(acessoResult.Value!, cancellationToken);
            return new AcessoAlunoDto
            {
                Id = entrada.Id,
                Aluno = aluno.ToDto(),
                DataHora = entrada.DataHora,
                Tipo = AppAcessoTipo.Entrada,
                PlanoDataFim = matricula.DataFim,
                DiasRestantesPlano = Math.Max(0, matricula.DataFim.DayNumber - hoje.DayNumber)
            };
        }

        // SAÍDA: informa o tempo que permaneceu na academia
        var ultimo = await acessoRepo.ObterUltimoAcesso(aluno.Id, cancellationToken);
        var saida = await acessoRepo.Adicionar(acessoResult.Value!, cancellationToken);
        return new AcessoAlunoDto
        {
            Id = saida.Id,
            Aluno = aluno.ToDto(),
            DataHora = saida.DataHora,
            Tipo = AppAcessoTipo.Saida,
            Permanencia = ultimo != null && saida.DataHora > ultimo.DataHora ? saida.DataHora - ultimo.DataHora : TimeSpan.Zero
        };
    }

    public async Task<IEnumerable<AcessoAlunoDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessos = (await _acessoRepoFactory().ObterAcessosPorAlunoPeriodo(null, hoje, hoje, cancellationToken)).OrderBy(a => a.DataHora).ToList();
        var lista = new List<AcessoAlunoDto>();
        foreach (var grupo in acessos.GroupBy(a => a.Aluno.Id))
        {
            var alunoDto = grupo.First().Aluno.ToDto();
            var doDia = grupo.ToList();
            for (int i = 0; i < doDia.Count; i++)
            {
                bool ehSaida = i % 2 == 1;
                lista.Add(new AcessoAlunoDto
                {
                    Id = doDia[i].Id,
                    Aluno = alunoDto,
                    DataHora = doDia[i].DataHora,
                    Tipo = ehSaida ? AppAcessoTipo.Saida : AppAcessoTipo.Entrada,
                    Permanencia = ehSaida ? doDia[i].DataHora - doDia[i - 1].DataHora : null
                });
            }
        }
        return [.. lista.OrderByDescending(a => a.DataHora)];
    }
}
