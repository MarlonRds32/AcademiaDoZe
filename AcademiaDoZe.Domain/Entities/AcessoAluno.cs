using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities
{
    public class AcessoAluno : Entity
    {
        public Aluno Aluno { get; private set; }

        public DateTime Entrada { get; private set; }

        public DateTime Saida { get; private set; }

        public AcessoAluno(
            int id,
            Aluno aluno,
            DateTime entrada,
            DateTime saida)
            : base(id)
        {
            Aluno = aluno;
            Entrada = entrada;
            Saida = saida;
        }
    }
}
