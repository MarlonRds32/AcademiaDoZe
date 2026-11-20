using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;

public interface IAcessoColaboradorService
{
    // Registra a passagem do colaborador: se não está na academia é entrada, se está é saída.
    Task<AcessoColaboradorDto> RegistrarAsync(string cpf, CancellationToken cancellationToken = default);
    // Registros de hoje, do mais recente para o mais antigo.
    Task<IEnumerable<AcessoColaboradorDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default);
}
