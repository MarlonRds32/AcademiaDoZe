using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Enums
{
    [Flags]
    public enum MatriculaRestricoes
    {
        None = 0,
        Diabetes = 1,
        PressaoAlta = 2,
        Labirintite = 4,
        Alergias = 8,
        ProblemasRespiratorios = 16,
        RemedioContinuo = 32
    }
}
