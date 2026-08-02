using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities
{
    public class Matricula : Entity
    {
        public Aluno Aluno { get; private set; }

        public MatriculaPlano Plano { get; private set; }

        public DateOnly DataInicio { get; private set; }

        public DateOnly DataFinal { get; private set; }

        public string Objetivo { get; private set; }

        public MatriculaRestricoes Restricoes { get; private set; }

        public string Observacoes { get; private set; }

        public Arquivo LaudoMedico { get; private set; }

        public Matricula(
            int id,
            Aluno aluno,
            MatriculaPlano plano,
            DateOnly dataInicio,
            DateOnly dataFinal,
            string objetivo,
            MatriculaRestricoes restricoes,
            string observacoes,
            Arquivo laudoMedico)
            : base(id)
        {
            Aluno = aluno;
            Plano = plano;
            DataInicio = dataInicio;
            DataFinal = dataFinal;
            Objetivo = objetivo;
            Restricoes = restricoes;
            Observacoes = observacoes;
            LaudoMedico = laudoMedico;
        }
    }
}
