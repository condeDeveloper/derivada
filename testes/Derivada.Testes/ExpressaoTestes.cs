using Conde.Derivada;
using Xunit;

namespace Conde.Derivada.Testes;

public class ExpressaoTestes
{
    [Fact]
    public void AAvaliacaoConfereNosQuatroOperadores()
    {
        var x = Expressao.X();
        var dois = Expressao.Numero(2);

        Assert.Equal(5, (x + dois).Avaliar(3), 1e-12);
        Assert.Equal(1, (x - dois).Avaliar(3), 1e-12);
        Assert.Equal(6, (x * dois).Avaliar(3), 1e-12);
        Assert.Equal(1.5, (x / dois).Avaliar(3), 1e-12);
    }

    [Fact]
    public void AsFuncoesElementaresConferem()
    {
        var x = Expressao.X();
        Assert.Equal(Math.Sin(0.5), Expressao.Sen(x).Avaliar(0.5), 1e-12);
        Assert.Equal(Math.Cos(0.5), Expressao.Cos(x).Avaliar(0.5), 1e-12);
        Assert.Equal(Math.Exp(0.5), Expressao.Exp(x).Avaliar(0.5), 1e-12);
        Assert.Equal(Math.Log(0.5), Expressao.Log(x).Avaliar(0.5), 1e-12);
        Assert.Equal(8, x.Elevado(3).Avaliar(2), 1e-12);
    }

    [Fact]
    public void AsVariaveisSaoLidasPorIndice()
    {
        var f = Expressao.X(0) * Expressao.X(2);
        Assert.Equal(12, f.Avaliar(3, 99, 4), 1e-12);
    }

    [Fact]
    public void AContagemDeVariaveisConfere()
    {
        Assert.Equal(1, Funcoes.Polinomio().Variaveis);
        Assert.Equal(2, Funcoes.Rosenbrock().Variaveis);
        Assert.Equal(7, Funcoes.SomaDeQuadrados(7).Variaveis);
    }

    [Fact]
    public void AContagemDeNosConfere()
    {
        var x = Expressao.X();
        Assert.Equal(1, x.Nos);
        Assert.Equal(3, (x + Expressao.Numero(1)).Nos);
        Assert.Equal(2, Expressao.Sen(x).Nos);
    }

    [Fact]
    public void ATextoDaExpressaoEhLegivel()
    {
        Assert.Equal("(x + 1)", (Expressao.X() + Expressao.Numero(1)).ToString());
        Assert.Equal("sen(x)", Expressao.Sen(Expressao.X()).ToString());
        Assert.Equal("x^3", Expressao.X().Elevado(3).ToString());
        Assert.Equal("x1", Expressao.X(1).ToString());
    }

    [Fact]
    public void ARosenbrockValeZeroNoMinimo()
    {
        Assert.Equal(0, Funcoes.Rosenbrock().Avaliar(1, 1), 1e-12);
        Assert.True(Funcoes.Rosenbrock().Avaliar(0, 0) > 0);
    }

    [Fact]
    public void OProdutoDeFatoresConfereNaMao()
    {
        // (x+1)(x+2)(x+3) em x = 0 e 1*2*3 = 6.
        Assert.Equal(6, Funcoes.Produto(3).Avaliar(0), 1e-12);
        Assert.Equal(24, Funcoes.Produto(3).Avaliar(1), 1e-12);
    }

    [Fact]
    public void ACadeiaDeSenosConfereNaMao()
    {
        Assert.Equal(Math.Sin(Math.Sin(Math.Sin(0.4))), Funcoes.Cadeia(3).Avaliar(0.4), 1e-12);
        Assert.Equal(0.4, Funcoes.Cadeia(0).Avaliar(0.4), 1e-12);
    }

    [Fact]
    public void AFracaoContinuaConfereNaMao()
    {
        // Um nivel: x / (1 + x). Em x = 1, da 0,5.
        Assert.Equal(0.5, Funcoes.Fracao(1).Avaliar(1), 1e-12);

        // Dois niveis: x / (1 + x/(1+x)). Em x = 1, da 1 / 1,5.
        Assert.Equal(1 / 1.5, Funcoes.Fracao(2).Avaliar(1), 1e-12);
    }

    [Fact]
    public void ASomaDeQuadradosConfereNaMao()
    {
        Assert.Equal(1 + 4 + 9, Funcoes.SomaDeQuadrados(3).Avaliar(1, 2, 3), 1e-12);
    }

    /// <summary>
    /// A funcao quase zero perde TODOS os digitos perto de zero, e isso nao e
    /// defeito da expressao: e a subtracao fazendo o que ela faz.
    /// </summary>
    [Fact]
    public void AQuaseZeroPerdeOsDigitos()
    {
        var f = Funcoes.QuaseZero();
        Assert.Equal(0, f.Avaliar(1e-16));
        Assert.True(f.Avaliar(1e-8) > 0);
    }
}
