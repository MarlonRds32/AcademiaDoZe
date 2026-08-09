using System;
using System.Collections.Generic;
using System.Text;
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;
// Marlon Rodrigues
namespace AcademiaDoZe.Domain.ValueObjects
{
    public record Senha
    {
        public string Valor { get; }
        private Senha(string valor)
        {
            Valor = valor;
        }
        public static Result<Senha> Criar(string valor)
        {
            if (NormalizadoService.TextoVazioOuNulo(valor))
                return Result<Senha>.Failure("Senha", "SENHA_OBRIGATORIA");

            var textoLimpo = NormalizadoService.LimparEDigitos(valor);

            if (textoLimpo.Length != 8)
                return Result<Senha>.Failure("Senha", "SENHA_TAMANHO_MINIMO");

            if (!textoLimpo.Any(char.IsUpper))
                return Result<Senha>.Failure("Senha", "SENHA_REQUER_MAIUSCULA");

            if (!textoLimpo.Any(char.IsLower))
                return Result<Senha>.Failure("Senha", "SENHA_REQUER_MINUSCULA");

            if (!textoLimpo.Any(char.IsDigit))
                return Result<Senha>.Failure("Senha", "SENHA_REQUER_NUMERO");

            return Result<Senha>.Success(new Senha(textoLimpo));
        }

        public override string ToString() => Valor;
    }
}
