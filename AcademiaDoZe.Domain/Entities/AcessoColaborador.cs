using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities
{
    public class AcessoColaborador : Entity
    {
        public Colaborador Colaborador { get; private set; }

        public DateTime Entrada { get; private set; }

        public DateTime Saida { get; private set; }

        public AcessoColaborador(
            int id,
            Colaborador colaborador,
            DateTime entrada,
            DateTime saida)
            : base(id)
        {
            Colaborador = colaborador;
            Entrada = entrada;
            Saida = saida;
        }
    }
}
