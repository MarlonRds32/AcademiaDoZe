using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities
{
    public class Logradouro : Entity
    {
        public string Pais { get; private set; }

        public string Estado { get; private set; }

        public string Cidade { get; private set; }

        public string Bairro { get; private set; }

        public string Nome { get; private set; }

        public Cep Cep { get; private set; }

        public Logradouro(
            int id,
            string pais,
            string estado,
            string cidade,
            string bairro,
            string nome,
            Cep cep)
            : base(id)
        {
            Pais = pais;
            Estado = estado;
            Cidade = cidade;
            Bairro = bairro;
            Nome = nome;
            Cep = cep;
        }
    }
}
