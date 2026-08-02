using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Arquivo
    {
        public string Nome { get; }

        public string Caminho { get; }

        public Arquivo(string nome, string caminho)
        {
            Nome = nome;
            Caminho = caminho;
        }
    }
}
