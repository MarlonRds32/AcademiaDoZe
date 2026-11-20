using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
// Marlon Rodrigues
namespace AcademiaDoZe.Infrastructure.Repositories;

/*
Cada linha de tb_acesso é um evento de passagem pela catraca.
Os registros do dia de uma pessoa se alternam: 1º = entrada, 2º = saída, 3º = entrada...
pessoa_tipo: 0 = Aluno, 1 = Colaborador
*/
public class AcessoAlunoRepository : BaseRepository, IAcessoAlunoRepository
{
    private const int PessoaTipo = 0;

    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }

    private const string BaseSelectQuery = "SELECT id_acesso, pessoa_id, data_hora FROM tb_acesso WHERE pessoa_tipo = @PessoaTipo";

    // Lê as linhas cruas da tabela, já ordenadas por data/hora
    private async Task<List<(int Id, int PessoaId, DateTime DataHora)>> LerLinhasAsync(int? pessoaId, DateTime? inicio, DateTime? fimExclusivo, CancellationToken cancellationToken)
    {
        string query = BaseSelectQuery;
        if (pessoaId.HasValue) query += " AND pessoa_id = @PessoaId";
        if (inicio.HasValue) query += " AND data_hora >= @Inicio";
        if (fimExclusivo.HasValue) query += " AND data_hora < @Fim";
        query += " ORDER BY data_hora";

        await using var command = await CreateCommandAsync(query, cancellationToken);
        command.AddParameter("@PessoaTipo", PessoaTipo, DbType.Int32);
        if (pessoaId.HasValue) command.AddParameter("@PessoaId", pessoaId.Value, DbType.Int32);
        if (inicio.HasValue) command.AddParameter("@Inicio", inicio.Value, DbType.DateTime);
        if (fimExclusivo.HasValue) command.AddParameter("@Fim", fimExclusivo.Value, DbType.DateTime);

        var linhas = new List<(int, int, DateTime)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            linhas.Add((reader.GetInt32Value("id_acesso"), reader.GetInt32Value("pessoa_id"), reader.GetDateTimeValue("data_hora")));
        }
        return linhas;
    }

    // Converte as linhas em entidades, carregando cada pessoa uma única vez
    private async Task<List<AcessoAluno>> MontarAsync(List<(int Id, int PessoaId, DateTime DataHora)> linhas, CancellationToken cancellationToken)
    {
        var acessos = new List<AcessoAluno>();
        if (linhas.Count == 0) return acessos;

        await using var pessoaRepo = new AlunoRepository(_connectionString, _databaseType);
        var cache = new Dictionary<int, Aluno?>();
        foreach (var linha in linhas)
        {
            if (!cache.TryGetValue(linha.PessoaId, out var pessoa))
            {
                pessoa = await pessoaRepo.ObterPorId(linha.PessoaId, cancellationToken);
                cache[linha.PessoaId] = pessoa;
            }
            if (pessoa == null) continue;

            var result = AcessoAluno.Criar(linha.Id, pessoa, linha.DataHora);
            if (result.IsSuccess) acessos.Add(result.Value!);
        }
        return acessos;
    }

    private static (DateTime? Inicio, DateTime? Fim) Periodo(DateOnly? inicio, DateOnly? fim) =>
        (inicio?.ToDateTime(TimeOnly.MinValue), fim?.AddDays(1).ToDateTime(TimeOnly.MinValue));

    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var linhas = (await LerLinhasAsync(null, null, null, cancellationToken)).Where(l => l.Id == id).ToList();
            return (await MontarAsync(linhas, cancellationToken)).FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso por ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            return await MontarAsync(await LerLinhasAsync(null, null, null, cancellationToken), cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipo, DbType.Int32);
            command.AddParameter("@PessoaId", entity.Aluno.Id, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO", "Falha ao obter ID inserido para o acesso.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO", $"Erro ao registrar acesso: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaId", entity.Aluno.Id, DbType.Int32);
            command.AddParameter("@DataHora", entity.DataHora, DbType.DateTime);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipo, DbType.Int32);
            int rows = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rows == 0)
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Acesso com ID {entity.Id} não encontrado para atualização.");
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO", $"Erro ao atualizar acesso ID {entity.Id}: {ex.Message}", ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "DELETE FROM tb_acesso WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            command.AddParameter("@PessoaTipo", PessoaTipo, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO", $"Erro ao remover acesso ID {id}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var (ini, fimExclusivo) = Periodo(inicio, fim);
            return await MontarAsync(await LerLinhasAsync(alunoId, ini, fimExclusivo, cancellationToken), cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos por período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var linhas = await LerLinhasAsync(alunoId, null, null, cancellationToken);
            if (linhas.Count == 0) return null;
            return (await MontarAsync([linhas[^1]], cancellationToken)).FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso: {ex.Message}", ex);
        }
    }

    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var (ini, fimExclusivo) = Periodo(hoje, hoje);
            var linhas = await LerLinhasAsync(alunoId, ini, fimExclusivo, cancellationToken);
            // quantidade ímpar de registros no dia = entrou e ainda não saiu
            return linhas.Count % 2 == 1;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ESTA_NA_ACADEMIA", $"Erro ao verificar presença do aluno ID {alunoId}: {ex.Message}", ex);
        }
    }

    // Agrupa por aluno e dia, devolvendo os pares (entrada, saída?) em ordem
    private static IEnumerable<(DateTime Entrada, DateTime? Saida)> Pares(IEnumerable<(int Id, int PessoaId, DateTime DataHora)> linhas)
    {
        foreach (var grupo in linhas.GroupBy(l => (l.PessoaId, l.DataHora.Date)))
        {
            var doDia = grupo.OrderBy(l => l.DataHora).ToList();
            for (int i = 0; i < doDia.Count; i += 2)
            {
                yield return (doDia[i].DataHora, i + 1 < doDia.Count ? doDia[i + 1].DataHora : null);
            }
        }
    }

    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var linhas = (await LerLinhasAsync(null, null, null, cancellationToken)).Where(l => l.DataHora.Month == mes);
            return Pares(linhas)
                .GroupBy(p => p.Entrada.Hour)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => new TimeOnly(g.Key, 0), g => g.Count());
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_HORARIO_MAIS_PROCURADO", $"Erro ao obter horário mais procurado do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default)
    {
        try
        {
            var linhas = (await LerLinhasAsync(null, null, null, cancellationToken)).Where(l => l.DataHora.Month == mes);
            var duracoes = Pares(linhas).Where(p => p.Saida.HasValue).Select(p => (p.Saida!.Value - p.Entrada).Ticks).ToList();
            var media = duracoes.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)duracoes.Average());
            return new Dictionary<int, TimeSpan> { [mes] = media };
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_PERMANENCIA_MEDIA", $"Erro ao obter permanência média do mês {mes}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default)
    {
        try
        {
            var desde = DateTime.Now.Date.AddDays(-dias);
            var comAcesso = (await LerLinhasAsync(null, desde, null, cancellationToken)).Select(l => l.PessoaId).ToHashSet();
            await using var alunoRepo = new AlunoRepository(_connectionString, _databaseType);
            var todos = await alunoRepo.ObterTodos(cancellationToken);
            return [.. todos.Where(a => !comAcesso.Contains(a.Id))];
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ALUNOS_SEM_ACESSO", $"Erro ao obter alunos sem acesso nos últimos {dias} dias: {ex.Message}", ex);
        }
    }
}
