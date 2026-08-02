using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Cep
    {
        public string Valor { get; }

        public Cep(string valor)
        {
            Valor = valor;
        }
    }
}
