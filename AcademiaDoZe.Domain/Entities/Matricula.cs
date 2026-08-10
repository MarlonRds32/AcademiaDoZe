using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.Common;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities
{
    public class Matricula : Entity
    {
        // encapsulamento das propriedades, aplicando imutabilidade
        public Aluno AlunoMatricula { get; private set; }
        public MatriculaPlano Plano { get; private set; }
        public DateOnly DataInicio { get; private set; }
        public DateOnly DataFim { get; private set; }
        public string Objetivo { get; private set; }
        public MatriculaRestricoes RestricoesMedicas { get; private set; }
        public string ObservacoesRestricoes { get; private set; }
        public Arquivo? LaudoMedico { get; private set; }
        // construtor privado para evitar instância direta
        private Matricula(int id, Aluno alunoMatricula, MatriculaPlano plano,

        DateOnly dataInicio, DateOnly dataFim,
        string objetivo, MatriculaRestricoes restricoesMedicas,
        Arquivo? laudoMedico, string observacoesRestricoes = "") : base(id)

        {
            AlunoMatricula = alunoMatricula;
            Plano = plano;
            DataInicio = dataInicio;
            DataFim = dataFim;
            Objetivo = objetivo;
            RestricoesMedicas = restricoesMedicas;
            LaudoMedico = laudoMedico;
            ObservacoesRestricoes = observacoesRestricoes;
        }

        public static Result<Matricula> Criar(
            int id,
            Aluno alunoMatricula,
            MatriculaPlano plano,
            DateOnly dataInicio,
            DateOnly dataFim,
            string objetivo,
            MatriculaRestricoes restricoesMedicas,
            Arquivo? laudoMedico,
            string observacoesRestricoes)
        {
            var notifications = new List<Notification>();

            if (alunoMatricula == null)
                notifications.Add(new Notification("AlunoMatricula", "ALUNO_MATRICULA_OBRIGATORIO"));

            if (!Enum.IsDefined(plano))
                notifications.Add(new Notification("Plano", "MATRICULA_PLANO_INVALIDO"));

            if (dataInicio == default)
                notifications.Add(new Notification("DataInicio", "DATA_INICIO_OBRIGATORIA"));

            if (dataFim == default)
                notifications.Add(new Notification("DataFim", "DATA_FIM_OBRIGATORIA"));
            else if (dataFim <= dataInicio)
                notifications.Add(new Notification("DataFim", "DATA_FIM_MENOR_OU_IGUAL_INICIO"));

            objetivo = NormalizadoService.LimparEspacos(objetivo);
            observacoesRestricoes = NormalizadoService.LimparEspacos(observacoesRestricoes);

            if (notifications.Count != 0)
                return Result<Matricula>.Failure(notifications);

            var matricula = new Matricula(
                id,
                alunoMatricula!,
                plano,
                dataInicio,
                dataFim,
                objetivo,
                restricoesMedicas,
                laudoMedico,
                observacoesRestricoes);

            return Result<Matricula>.Success(matricula);
        }
    }
}
