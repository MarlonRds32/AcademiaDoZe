using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Senha
    {
        public string Valor { get; }

        public Senha(string valor)
        {
            Valor = valor;
        }
    }
}
