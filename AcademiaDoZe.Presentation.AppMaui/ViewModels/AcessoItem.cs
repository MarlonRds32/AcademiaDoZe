using AcademiaDoZe.Application.DTOs;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

// Linha exibida na tela de Entrada / Saída (último registro e registros do dia)
public class AcessoItem
{
    public required string Nome { get; set; }
    public required string Cpf { get; set; }
    public ArquivoDto? Foto { get; set; }
    public required DateTime DataHora { get; set; }
    public required bool IsEntrada { get; set; }
    public bool IsSaida => !IsEntrada;
    // "Aluno" ou "Colaborador", já no idioma atual
    public required string PessoaTexto { get; set; }
    // "Entrada" ou "Saída", já no idioma atual
    public required string TipoTexto { get; set; }
    // tempo de plano restante, permanência ou horas trabalhadas
    public string Detalhe { get; set; } = string.Empty;
    public bool HasDetalhe => !string.IsNullOrWhiteSpace(Detalhe);
    public string Hora => DataHora.ToString("T", CultureInfo.CurrentCulture);
    public string DataHoraTexto => DataHora.ToString("g", CultureInfo.CurrentCulture);
}
