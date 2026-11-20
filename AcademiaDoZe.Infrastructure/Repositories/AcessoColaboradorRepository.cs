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
public class AcessoColaboradorRepository : BaseRepository, IAcessoColaboradorRepository
{
    private const int PessoaTipo = 1;

    public AcessoColaboradorRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
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
    private async Task<List<AcessoColaborador>> MontarAsync(List<(int Id, int PessoaId, DateTime DataHora)> linhas, CancellationToken cancellationToken)
    {
        var acessos = new List<AcessoColaborador>();
        if (linhas.Count == 0) return acessos;

        await using var pessoaRepo = new ColaboradorRepository(_connectionString, _databaseType);
        var cache = new Dictionary<int, Colaborador?>();
        foreach (var linha in linhas)
        {
            if (!cache.TryGetValue(linha.PessoaId, out var pessoa))
            {
                pessoa = await pessoaRepo.ObterPorId(linha.PessoaId, cancellationToken);
                cache[linha.PessoaId] = pessoa;
            }
            if (pessoa == null) continue;

            var result = AcessoColaborador.Criar(linha.Id, pessoa, linha.DataHora);
            if (result.IsSuccess) acessos.Add(result.Value!);
        }
        return acessos;
    }

    private static (DateTime? Inicio, DateTime? Fim) Periodo(DateOnly? inicio, DateOnly? fim) =>
        (inicio?.ToDateTime(TimeOnly.MinValue), fim?.AddDays(1).ToDateTime(TimeOnly.MinValue));

    public async Task<AcessoColaborador?> ObterPorId(int id, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<AcessoColaborador>> ObterTodos(CancellationToken cancellationToken = default)
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

    public async Task<AcessoColaborador> Adicionar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES (@PessoaTipo, @PessoaId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaTipo", PessoaTipo, DbType.Int32);
            command.AddParameter("@PessoaId", entity.Colaborador.Id, DbType.Int32);
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

    public async Task<AcessoColaborador> Atualizar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "UPDATE tb_acesso SET pessoa_id = @PessoaId, data_hora = @DataHora WHERE id_acesso = @Id AND pessoa_tipo = @PessoaTipo";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@PessoaId", entity.Colaborador.Id, DbType.Int32);
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

    public async Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var (ini, fimExclusivo) = Periodo(inicio, fim);
            return await MontarAsync(await LerLinhasAsync(colaboradorId, ini, fimExclusivo, cancellationToken), cancellationToken);
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos por período: {ex.Message}", ex);
        }
    }

    public async Task<AcessoColaborador?> ObterUltimoAcesso(int colaboradorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var linhas = await LerLinhasAsync(colaboradorId, null, null, cancellationToken);
            if (linhas.Count == 0) return null;
            return (await MontarAsync([linhas[^1]], cancellationToken)).FirstOrDefault();
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso: {ex.Message}", ex);
        }
    }

    public async Task<TimeSpan> ObterHorasTrabalhadasNoDia(int colaboradorId, DateOnly data, CancellationToken cancellationToken = default)
    {
        try
        {
            var (ini, fimExclusivo) = Periodo(data, data);
            var linhas = await LerLinhasAsync(colaboradorId, ini, fimExclusivo, cancellationToken);
            var total = TimeSpan.Zero;
            for (int i = 0; i < linhas.Count; i += 2)
            {
                var entrada = linhas[i].DataHora;
                DateTime saida;
                if (i + 1 < linhas.Count)
                    saida = linhas[i + 1].DataHora;
                else if (data == DateOnly.FromDateTime(DateTime.Now))
                    saida = DateTime.Now; // ainda está na academia: conta até agora
                else
                    saida = entrada; // dia passado sem registro de saída: não soma
                if (saida > entrada) total += saida - entrada;
            }
            return total;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_HORAS_TRABALHADAS", $"Erro ao obter horas trabalhadas do colaborador ID {colaboradorId}: {ex.Message}", ex);
        }
    }
}
