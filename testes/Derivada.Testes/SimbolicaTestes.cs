using Conde.Derivada;
using Xunit;

namespace Conde.Derivada.Testes;

/// <summary>
/// O juiz testado antes de julgar. Se a derivada simbolica estiver errada, os
/// outros tres metodos discordam dela e eu culparia os tres.
/// </summary>
public class SimbolicaTestes
{
    private const double Folga = 1e-12;

    [Fact]
    public void ADerivadaDaConstanteEZero()
    {
        Assert.Equal(0, Simbolica.Derivar(Expressao.Numero(7)).Avaliar(3), Folga);
    }

    [Fact]
    public void ADerivadaDaVariavelEUm()
    {
        Assert.Equal(1, Simbolica.Derivar(Expressao.X()).Avaliar(3), Folga);
    }

    /// <summary>A derivada em relacao a OUTRA variavel e zero.</summary>
    [Fact]
    public void ADerivadaCruzadaEZero()
    {
        Assert.Equal(0, Simbolica.Derivar(Expressao.X(0), variavel: 1).Avaliar(3, 4), Folga);
    }

    /// <summary>x^3 - 2x + 1 tem derivada 3x^2 - 2, que da para conferir de cabeca.</summary>
    [Fact]
    public void OPolinomioConfereDeCabeca()
    {
        var derivada = Simbolica.Derivar(Funcoes.Polinomio());
        foreach (var x in new[] { -2.0, 0.0, 1.5, 3.0 })
            Assert.Equal(3 * x * x - 2, derivada.Avaliar(x), Folga);
    }

    /// <summary>A derivada de sen(x)*exp(x) e (cos(x) + sen(x))*exp(x).</summary>
    [Fact]
    public void ARegraDoProdutoConfere()
    {
        var derivada = Simbolica.Derivar(Funcoes.SenoVezesExponencial());
        foreach (var x in new[] { 0.0, 0.7, 2.0 })
            Assert.Equal((Math.Cos(x) + Math.Sin(x)) * Math.Exp(x), derivada.Avaliar(x), 1e-10);
    }

    /// <summary>A derivada de sen(sen(sen(x))) e o produto dos tres cossenos.</summary>
    [Fact]
    public void ARegraDaCadeiaConfere()
    {
        var derivada = Simbolica.Derivar(Funcoes.Cadeia(3));
        const double x = 0.4;
        var esperado = Math.Cos(Math.Sin(Math.Sin(x))) * Math.Cos(Math.Sin(x)) * Math.Cos(x);
        Assert.Equal(esperado, derivada.Avaliar(x), 1e-12);
    }

    [Fact]
    public void ARegraDoQuocienteConfere()
    {
        var f = Expressao.X() / (Expressao.Numero(1) + Expressao.X());
        var derivada = Simbolica.Derivar(f);
        foreach (var x in new[] { 0.5, 2.0, 5.0 })
            Assert.Equal(1 / ((1 + x) * (1 + x)), derivada.Avaliar(x), Folga);
    }

    [Fact]
    public void ADerivadaDoLogaritmoEUmSobreX()
    {
        var derivada = Simbolica.Derivar(Expressao.Log(Expressao.X()));
        Assert.Equal(1 / 3.0, derivada.Avaliar(3), Folga);
    }

    /// <summary>A segunda derivada e so derivar de novo.</summary>
    [Fact]
    public void ASegundaDerivadaEDerivarDeNovo()
    {
        var segunda = Simbolica.Derivar(Simbolica.Derivar(Funcoes.Polinomio()));
        foreach (var x in new[] { -1.0, 2.0 })
            Assert.Equal(6 * x, segunda.Avaliar(x), Folga);
    }

    /// <summary>A simplificacao nao pode mudar o VALOR em ponto nenhum.</summary>
    [Fact]
    public void ASimplificacaoNaoMudaOValor()
    {
        foreach (var (_, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var derivada = Simbolica.Derivar(f);
            var simplificada = Simbolica.Simplificar(derivada);
            foreach (var x in new[] { ponto, ponto + 0.3, ponto - 0.2 })
                Assert.Equal(derivada.Avaliar(x), simplificada.Avaliar(x), 1e-10);
        }
    }

    [Fact]
    public void ASimplificacaoEncolheAExpressao()
    {
        var derivada = Simbolica.Derivar(Funcoes.Produto(5));
        Assert.True(Simbolica.Simplificar(derivada).Nos < derivada.Nos);
    }

    /// <summary>
    /// O que me corrigiu: o inchaco e QUADRATICO na contagem de arvore e
    /// LINEAR na contagem de nos distintos, porque as duas copias do denominador
    /// sao o mesmo objeto.
    /// </summary>
    [Fact]
    public void OInchacoEhQuadraticoNaArvoreELinearNoGrafo()
    {
        var pequena = Simbolica.Derivar(Funcoes.Fracao(8));
        var grande = Simbolica.Derivar(Funcoes.Fracao(16));

        var crescimentoArvore = grande.Nos / (double)pequena.Nos;
        var crescimentoGrafo = grande.NosDistintos / (double)pequena.NosDistintos;

        Assert.True(crescimentoArvore > 3, "dobrar os niveis deveria mais que triplicar a arvore");
        Assert.True(crescimentoGrafo < 2.2, "e o grafo deveria apenas dobrar");
        Assert.True(grande.Nos > 5 * grande.NosDistintos);
    }

    [Fact]
    public void AContagemDeNosDistintosNaoConfundeObjetosIguais()
    {
        var x = Expressao.X();
        var f = x + x;
        Assert.Equal(3, f.Nos);
        Assert.Equal(2, f.NosDistintos);
    }

    [Fact]
    public void OGradienteSimbolicoTemUmaDerivadaPorVariavel()
    {
        var gradiente = Simbolica.Gradiente(Funcoes.Rosenbrock(), 2);
        Assert.Equal(2, gradiente.Length);

        double[] ponto = [0.5, 2.0];
        Assert.Equal(-351, gradiente[0].Avaliar(ponto), 1e-9);
        Assert.Equal(350, gradiente[1].Avaliar(ponto), 1e-9);
    }

    /// <summary>No minimo da Rosenbrock, em (1,1), o gradiente e zero.</summary>
    [Fact]
    public void NoMinimoDaRosenbrockOGradienteSomem()
    {
        var gradiente = Simbolica.Gradiente(Funcoes.Rosenbrock(), 2);
        double[] minimo = [1.0, 1.0];
        Assert.Equal(0, gradiente[0].Avaliar(minimo), Folga);
        Assert.Equal(0, gradiente[1].Avaliar(minimo), Folga);
    }
}
