using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Endereco
    {
        public Cep Cep { get; }

        public string Pais { get; }

        public string Estado { get; }

        public string Cidade { get; }

        public string Bairro { get; }

        public string NomeLogradouro { get; }

        public int Numero { get; }

        public string Complemento { get; }

        public Endereco(
            Cep cep,
            string pais,
            string estado,
            string cidade,
            string bairro,
            string nomeLogradouro,
            int numero,
            string complemento)
        {
            Cep = cep;
            Pais = pais;
            Estado = estado;
            Cidade = cidade;
            Bairro = bairro;
            NomeLogradouro = nomeLogradouro;
            Numero = numero;
            Complemento = complemento;
        }
    }
}
