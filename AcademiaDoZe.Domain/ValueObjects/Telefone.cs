using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Telefone
    {
        public string Valor { get; }

        public Telefone(string valor)
        {
            Valor = valor;
        }
    }
}
