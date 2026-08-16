using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.Common;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.Entities;

public class Matricula : Entity, IAggregateRoot
{
    public Aluno AlunoMatricula { get; private set; }
    public MatriculaPlano Plano { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public string Objetivo { get; private set; }
    public MatriculaRestricoes RestricoesMedicas { get; private set; }
    public string ObservacoesRestricoes { get; private set; }
    public Arquivo? LaudoMedico { get; private set; }

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
        string objetivo,
        MatriculaRestricoes restricoesMedicas,
        Arquivo? laudoMedico,
        string? observacoesRestricoes = null) 
    {
        var notifications = new List<Notification>();

        if (alunoMatricula == null)
            notifications.Add(new Notification("AlunoMatricula", "ALUNO_MATRICULA_OBRIGATORIO"));

        if (!Enum.IsDefined(plano))
            notifications.Add(new Notification("Plano", "PLANO_INVALIDO")); 

        if (dataInicio == default)
            notifications.Add(new Notification("DataInicio", "DATA_INICIO_OBRIGATORIO"));

       

        objetivo = NormalizadoService.LimparEspacos(objetivo);
        observacoesRestricoes = NormalizadoService.LimparEspacos(observacoesRestricoes ?? string.Empty);

        
        if (alunoMatricula != null && laudoMedico == null)
        {
            var refData = dataInicio == default ? DateOnly.FromDateTime(DateTime.Today) : dataInicio;
            var idade = refData.Year - alunoMatricula.DataNascimento.Year;
            if (refData < alunoMatricula.DataNascimento.AddYears(idade)) idade--;

            if (idade < 16)
                notifications.Add(new Notification("LaudoMedico", "MENOR_16_LAUDO_OBRIGATORIO"));
        }


        if (restricoesMedicas != MatriculaRestricoes.None && laudoMedico == null)
            notifications.Add(new Notification("LaudoMedico", "RESTRICOES_LAUDO_OBRIGATORIO"));

        if (notifications.Count != 0)
            return Result<Matricula>.Failure(notifications);

        
        var meses = plano switch
        {
            MatriculaPlano.Mensal => 1,
            MatriculaPlano.Trimestral => 3,
            MatriculaPlano.Semestral => 6,
            MatriculaPlano.Anual => 12,
            _ => 0
        };
        var dataFim = dataInicio.AddMonths(meses);

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