using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Email
    {
        public string Valor { get; }

        public Email(string valor)
        {
            Valor = valor;
        }
    }
}
