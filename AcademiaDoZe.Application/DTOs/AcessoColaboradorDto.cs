using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;
/*Marlon Rodrigues*/
public class AcessoColaboradorDto
{
    public int Id { get; set; }
    public required ColaboradorDto Colaborador { get; set; }
    public required DateTime DataHora { get; set; }
    public required AppAcessoTipo Tipo { get; set; }
    // Soma de todos os períodos trabalhados no dia
    public TimeSpan HorasTrabalhadasHoje { get; set; }
    // 8 horas para CLT, 6 horas para estágio
    public TimeSpan LimiteDiario { get; set; }
}
