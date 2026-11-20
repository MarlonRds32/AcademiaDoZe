namespace AcademiaDoZe.Application.Exceptions;

// Exceção de regra de negócio do controle de acesso.
// Codigo identifica a regra (usado na apresentação para buscar a mensagem traduzida) e Args traz os valores da mensagem.
public class RegraAcessoException : InvalidOperationException
{
    public string Codigo { get; }
    public object[] Args { get; }

    public RegraAcessoException(string codigo, string mensagem, params object[] args) : base(mensagem)
    {
        Codigo = codigo;
        Args = args ?? [];
    }
}
