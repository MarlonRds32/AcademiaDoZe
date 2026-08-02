using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Cpf
    {
        public string Valor { get; }

        public Cpf(string valor)
        {
            Valor = valor;
        }
    }
}
