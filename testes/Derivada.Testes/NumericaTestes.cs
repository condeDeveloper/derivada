using Conde.Derivada;
using Xunit;

namespace Conde.Derivada.Testes;

public class NumericaTestes
{
    private static readonly Expressao F = Funcoes.SenoVezesExponencial();
    private static readonly double[] Ponto = [0.7];
    private static readonly double Certo = Simbolica.Derivar(F).Avaliar(Ponto);

    /// <summary>
    /// O V: o erro desce, chega ao fundo e SOBE. Os dois lados precisam
    /// aparecer, senao o achado vira "passo menor e melhor".
    /// </summary>
    [Fact]
    public void OErroDaDiferencaAdianteFazUmV()
    {
        var grande = Math.Abs(Numerica.Adiante(F, Ponto, 0, 1e-1) - Certo);
        var bom = Math.Abs(Numerica.Adiante(F, Ponto, 0, 1e-8) - Certo);
        var pequeno = Math.Abs(Numerica.Adiante(F, Ponto, 0, 1e-16) - Certo);

        Assert.True(bom < grande);
        Assert.True(bom < pequeno);
    }

    /// <summary>
    /// O fundo do V da diferenca adiante cai na RAIZ de eps, que e o que a
    /// teoria diz, e o erro ali e da mesma ordem do passo.
    /// </summary>
    [Fact]
    public void OFundoDoVAdianteBateComARaizDeEps()
    {
        var (passo, erro) = Numerica.MelhorPasso(Numerica.Adiante, F, Ponto, 0, Certo);
        Assert.InRange(passo, 1e-9, 1e-7);
        Assert.InRange(erro, 1e-10, 1e-6);
    }

    /// <summary>
    /// E o da central cai na raiz CUBICA de eps, com um erro mil vezes menor. Uma
    /// avaliacao a mais compra tres digitos.
    /// </summary>
    [Fact]
    public void OFundoDoVCentralBateComARaizCubicaDeEps()
    {
        var (passo, erro) = Numerica.MelhorPasso(Numerica.Central, F, Ponto, 0, Certo);
        var (_, erroAdiante) = Numerica.MelhorPasso(Numerica.Adiante, F, Ponto, 0, Certo);

        Assert.InRange(passo, 1e-6, 1e-4);
        Assert.True(erro < erroAdiante / 1000);
    }

    /// <summary>
    /// O passo complexo nao tem V: o erro so DESCE e depois fica no chao, porque
    /// nao ha subtracao nenhuma para cancelar digitos.
    /// </summary>
    [Fact]
    public void OPassoComplexoNaoTemV()
    {
        foreach (var potencia in new[] { -8, -20, -60, -100, -200 })
        {
            var erro = Math.Abs(Numerica.Complexo(F, Ponto, 0, Math.Pow(10, potencia)) - Certo);
            Assert.True(erro < 1e-14, $"1e{potencia} deu {erro:E2}");
        }
    }

    /// <summary>
    /// E no mesmo passo em que a diferenca adiante ja nao devolve nada, o passo
    /// complexo esta exato. O contraste num teste so.
    /// </summary>
    [Fact]
    public void NoMesmoPassoUmaMorreEAOutraAcerta()
    {
        const double passo = 1e-30;
        Assert.True(Math.Abs(Numerica.Adiante(F, Ponto, 0, passo) - Certo) > 1);
        Assert.True(Math.Abs(Numerica.Complexo(F, Ponto, 0, passo) - Certo) < 1e-14);
    }

    [Fact]
    public void AsDiferencasAcertamComOMelhorPassoEmTodasAsFuncoes()
    {
        foreach (var (nome, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var x = new[] { ponto };
            var certo = Simbolica.Derivar(f).Avaliar(x);
            var (_, erro) = Numerica.MelhorPasso(Numerica.Central, f, x, 0, certo);
            Assert.True(erro < 1e-6 * Math.Max(1, Math.Abs(certo)), nome);
        }
    }

    [Fact]
    public void OComplexoAcertaEmTodasAsFuncoes()
    {
        foreach (var (nome, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var x = new[] { ponto };
            var certo = Simbolica.Derivar(f).Avaliar(x);
            var erro = Math.Abs(Numerica.Complexo(f, x, 0, 1e-200) - certo);
            Assert.True(erro < 1e-12 * Math.Max(1, Math.Abs(certo)), $"{nome}: {erro:E2}");
        }
    }

    /// <summary>
    /// O que me corrigiu: exp(x) - 1 perto de zero perde TODOS os digitos no
    /// valor, e a derivada continua certa nos tres metodos. O cancelamento da
    /// funcao nao contamina a derivada, porque a derivada e outra conta.
    /// </summary>
    [Fact]
    public void OCancelamentoDaFuncaoNaoContaminaADerivada()
    {
        var f = Funcoes.QuaseZero();
        double[] x = [1e-16];

        Assert.Equal(0, f.Avaliar(x));

        Assert.Equal(Math.Exp(1e-16), Avante.Derivada(f, x, 0), 1e-14);
        Assert.Equal(Math.Exp(1e-16), Reversa.Gradiente(f, x)[0], 1e-14);
        Assert.Equal(Math.Exp(1e-16), Numerica.Complexo(f, x, 0, 1e-200), 1e-14);
    }

    [Fact]
    public void AsDerivadasParciaisUsamAVariavelCerta()
    {
        var f = Funcoes.Rosenbrock();
        double[] ponto = [0.5, 2.0];
        var gradiente = Simbolica.Gradiente(f, 2);

        for (var i = 0; i < 2; i++)
        {
            var certo = gradiente[i].Avaliar(ponto);
            Assert.Equal(certo, Numerica.Complexo(f, ponto, i, 1e-200), 1e-9);
            Assert.Equal(certo, Numerica.Central(f, ponto, i, 1e-5), 1e-6);
        }
    }

    /// <summary>
    /// No dual, e*e = 0 e o termo de segunda ordem SOME; no complexo, i*i = -1 e
    /// ele so fica pequeno. E dai que vem o erro residual do passo complexo, e e
    /// por isso que os dois tipos sao separados neste repositorio.
    /// </summary>
    [Fact]
    public void ODualEhExatoEOComplexoTemResiduoDeSegundaOrdem()
    {
        var f = Funcoes.Polinomio();
        double[] x = [1.5];
        var certo = Simbolica.Derivar(f).Avaliar(x);

        Assert.Equal(certo, Avante.Derivada(f, x, 0));

        var comPassoGrande = Numerica.Complexo(f, x, 0, 1e-2);
        Assert.NotEqual(certo, comPassoGrande);
        Assert.True(Math.Abs(comPassoGrande - certo) < 1e-3);
    }
}
