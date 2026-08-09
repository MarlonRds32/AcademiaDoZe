using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities
{
    public class AcessoColaborador : Entity
    {
        public Colaborador Colaborador { get; private set; }
        public DateTime DataHora { get; private set; }
        private AcessoColaborador(int id, Colaborador colaborador, DateTime dataHora) : base(id)
        {
            Colaborador = colaborador;
            DataHora = dataHora;
        }

    }
}
