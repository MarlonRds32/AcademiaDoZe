using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;

public interface IAcessoAlunoService
{
    // Registra a passagem do aluno: se não está na academia é entrada, se está é saída.
    Task<AcessoAlunoDto> RegistrarAsync(string cpf, CancellationToken cancellationToken = default);
    // Registros de hoje, do mais recente para o mais antigo.
    Task<IEnumerable<AcessoAlunoDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default);
}
