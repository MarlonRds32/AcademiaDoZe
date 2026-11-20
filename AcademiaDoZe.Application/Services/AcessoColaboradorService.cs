using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Exceptions;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
namespace AcademiaDoZe.Application.Services;
/*Marlon Rodrigues*/
public class AcessoColaboradorService : IAcessoColaboradorService
{
    private readonly Func<IAcessoColaboradorRepository> _acessoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;

    public AcessoColaboradorService(Func<IAcessoColaboradorRepository> acessoRepoFactory, Func<IColaboradorRepository> colaboradorRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
    }

    // Limite diário: 8 horas se for CLT, 6 horas se for estágio
    private static TimeSpan LimiteDiario(ColaboradorVinculo vinculo) =>
        vinculo == ColaboradorVinculo.Estagio ? TimeSpan.FromHours(6) : TimeSpan.FromHours(8);

    public async Task<AcessoColaboradorDto> RegistrarAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var cpfResult = Cpf.Criar(new string([.. (cpf ?? string.Empty).Where(char.IsDigit)]));
        if (cpfResult.IsFailure)
            throw new RegraAcessoException("CPF_INVALIDO", "CPF inválido.");

        var colaborador = await _colaboradorRepoFactory().ObterPorCpf(cpfResult.Value!, cancellationToken)
            ?? throw new RegraAcessoException("COLABORADOR_NAO_ENCONTRADO", "Colaborador não encontrado para o CPF informado.");

        var agora = DateTime.Now;
        var hoje = DateOnly.FromDateTime(agora);
        var acessoRepo = _acessoRepoFactory();
        var limite = LimiteDiario(colaborador.Vinculo);

        var acessosHoje = (await acessoRepo.ObterAcessosPorColaboradorPeriodo(colaborador.Id, hoje, hoje, cancellationToken)).ToList();
        bool estaNaAcademia = acessosHoje.Count % 2 == 1;

        var acessoResult = AcessoColaborador.Criar(0, colaborador, agora);
        if (acessoResult.IsFailure)
        {
            if (acessoResult.Notifications.Any(n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO"))
                throw new RegraAcessoException("FORA_HORARIO_FUNCIONAMENTO", "A academia funciona das 06:00 às 22:00.");
            throw new RegraAcessoException("ACESSO_INVALIDO", string.Join(", ", acessoResult.Notifications.Select(n => n.Mensagem)));
        }

        if (!estaNaAcademia)
        {
            // ENTRADA: valida se já não ultrapassou o limite de horas do dia
            var horasAntes = await acessoRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, hoje, cancellationToken);
            if (horasAntes >= limite)
                throw new RegraAcessoException("LIMITE_HORAS_ATINGIDO",
                    $"O colaborador {colaborador.Nome} já atingiu o limite diário de {limite.TotalHours:0} horas.", colaborador.Nome, (int)limite.TotalHours);
        }

        var registrado = await acessoRepo.Adicionar(acessoResult.Value!, cancellationToken);
        // na saída, soma todos os registros do dia
        var horasHoje = await acessoRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, hoje, cancellationToken);

        return new AcessoColaboradorDto
        {
            Id = registrado.Id,
            Colaborador = colaborador.ToDto(),
            DataHora = registrado.DataHora,
            Tipo = estaNaAcademia ? AppAcessoTipo.Saida : AppAcessoTipo.Entrada,
            HorasTrabalhadasHoje = horasHoje,
            LimiteDiario = limite
        };
    }

    public async Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessos = (await _acessoRepoFactory().ObterAcessosPorColaboradorPeriodo(null, hoje, hoje, cancellationToken)).OrderBy(a => a.DataHora).ToList();
        var lista = new List<AcessoColaboradorDto>();
        foreach (var grupo in acessos.GroupBy(a => a.Colaborador.Id))
        {
            var colaborador = grupo.First().Colaborador;
            var colaboradorDto = colaborador.ToDto();
            var limite = LimiteDiario(colaborador.Vinculo);
            var doDia = grupo.ToList();
            var acumulado = TimeSpan.Zero;
            for (int i = 0; i < doDia.Count; i++)
            {
                bool ehSaida = i % 2 == 1;
                if (ehSaida) acumulado += doDia[i].DataHora - doDia[i - 1].DataHora;
                lista.Add(new AcessoColaboradorDto
                {
                    Id = doDia[i].Id,
                    Colaborador = colaboradorDto,
                    DataHora = doDia[i].DataHora,
                    Tipo = ehSaida ? AppAcessoTipo.Saida : AppAcessoTipo.Entrada,
                    HorasTrabalhadasHoje = acumulado,
                    LimiteDiario = limite
                });
            }
        }
        return [.. lista.OrderByDescending(a => a.DataHora)];
    }
}
