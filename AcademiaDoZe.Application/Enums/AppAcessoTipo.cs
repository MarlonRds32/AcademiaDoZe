using System.ComponentModel.DataAnnotations;
namespace AcademiaDoZe.Application.Enums;

public enum AppAcessoTipo
{
    [Display(Name = "Entrada")]
    Entrada = 0,
    [Display(Name = "Saída")]
    Saida = 1
}
