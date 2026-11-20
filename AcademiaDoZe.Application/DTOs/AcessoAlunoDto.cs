using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;
/*Marlon Rodrigues*/
public class AcessoAlunoDto
{
    public int Id { get; set; }
    public required AlunoDto Aluno { get; set; }
    public required DateTime DataHora { get; set; }
    public required AppAcessoTipo Tipo { get; set; }
    // Preenchidos na entrada: até quando vai o plano e quantos dias faltam
    public DateOnly? PlanoDataFim { get; set; }
    public int? DiasRestantesPlano { get; set; }
    // Preenchido na saída: tempo que permaneceu na academia
    public TimeSpan? Permanencia { get; set; }
}
