using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
namespace AcademiaDoZe.Domain.Tests.Entities;
// Marlon Rodrigues
public class ColaboradorTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar(new byte[] { 1, 2, 3 }).Value!;
    [Theory(DisplayName = "Colaborador: data admissao obrigatória -> DATA_ADMISSAO_OBRIGATORIO")]
    [InlineData(true)]
    [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_DataAdmissaoPadrao(bool useDefault)
    {
        var dataAdmissao = useDefault ? default(DateOnly) : DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
        var result = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), dataAdmissao, ColaboradorTipo.Atendente, ColaboradorVinculo.CLT);
        if (useDefault)
        {
            Assert.True(result.IsFailure);
            Assert.NotEmpty(result.Notifications);
            Assert.Contains(result.Notifications, n => n.Mensagem == "DATA_ADMISSAO_OBRIGATORIO");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }
    [Theory(DisplayName = "Colaborador: Administrador com vinculo inválido -> ADMINISTRADOR_CLT_INVALIDO")]
    [InlineData(ColaboradorTipo.Administrador, ColaboradorVinculo.Estagio)]
    [InlineData(ColaboradorTipo.Administrador, ColaboradorVinculo.CLT)]
    public void Deve_Falhar_Criacao_Quando_AdminComVinculoInvalido(ColaboradorTipo tipo, ColaboradorVinculo vinc)
    {
        var result = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), tipo, vinc);
        if (tipo == ColaboradorTipo.Administrador && vinc == ColaboradorVinculo.Estagio)
        {
            Assert.True(result.IsFailure);
            Assert.NotEmpty(result.Notifications);
            Assert.Contains(result.Notifications, n => n.Mensagem == "ADMINISTRADOR_CLT_INVALIDO");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }
    [Theory(DisplayName = "Colaborador: data admissao futura -> DATA_ADMISSAO_MAIOR_ATUAL")]
    [InlineData(1)]
    [InlineData(-1)]
    public void Deve_Falhar_Criacao_Quando_DataAdmissaoFutura(int daysOffset)
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(daysOffset));
        var result = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), date, ColaboradorTipo.Atendente, ColaboradorVinculo.CLT);
        if (daysOffset > 0)
        {
            Assert.True(result.IsFailure);
            Assert.NotEmpty(result.Notifications);
            Assert.Contains(result.Notifications, n => n.Mensagem == "DATA_ADMISSAO_MAIOR_QUE_ATUAL");
        }
        else
        {
            Assert.True(result.IsSuccess);
        }
    }
    [Theory(DisplayName = "Colaborador: tipo ou vínculo inválido -> valida enum inválido")]
    [InlineData(999, 1)]
    [InlineData(1, 999)]
    public void Deve_Falhar_Criacao_Quando_TipoOuVinculoInvalido(int tipoValue, int vincValue)
    {
        var tipo = (ColaboradorTipo)tipoValue;
        var vinc = (ColaboradorVinculo)vincValue;
        var result = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), tipo, vinc);
        Assert.True(result.IsFailure);
        Assert.NotEmpty(result.Notifications);
    }

    [Fact(DisplayName = "Colaborador: tipo inválido isoladamente -> TIPO_COLABORADOR_INVALIDO")]
    public void Deve_Falhar_Quando_ApenasTipoInvalido()
    {
        var logradouro = Logradouro.Criar(1, "12345-678", "Rua", "Bairro", "Cidade", "SP", "Brasil").Value!;
        var foto = Arquivo.Criar(new byte[] { 1 }).Value!;
        var tipoInvalido = (ColaboradorTipo)999;

        var result = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
            "(11) 91234-5678", "user@example.com", logradouro, "123", "", "Abcdef", foto,
            DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), tipoInvalido, ColaboradorVinculo.CLT);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "TIPO_COLABORADOR_INVALIDO");
    }

    [Fact(DisplayName = "Colaborador: vínculo inválido isoladamente -> VINCULO_COLABORADOR_INVALIDO")]
    public void Deve_Falhar_Quando_ApenasVinculoInvalido()
    {
        var logradouro = Logradouro.Criar(1, "12345-678", "Rua", "Bairro", "Cidade", "SP", "Brasil").Value!;
        var foto = Arquivo.Criar(new byte[] { 1 }).Value!;
        var vinculoInvalido = (ColaboradorVinculo)999;

        var result = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
            "(11) 91234-5678", "user@example.com", logradouro, "123", "", "Abcdef", foto,
            DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), ColaboradorTipo.Atendente, vinculoInvalido);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "VINCULO_COLABORADOR_INVALIDO");
    }






}