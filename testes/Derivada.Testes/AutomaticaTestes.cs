using Conde.Derivada;
using Xunit;

namespace Conde.Derivada.Testes;

public class AutomaticaTestes
{
    /// <summary>
    /// A regra do produto SAI do e*e = 0, sem ninguem escrever. Este teste
    /// confere isso na mao, com um dual montado a dedo.
    /// </summary>
    [Fact]
    public void ORegraDoProdutoSaiDoQuadradoZero()
    {
        var a = new Dual(2, 3);
        var b = new Dual(5, 7);
        var produto = a * b;

        Assert.Equal(10, produto.Valor);
        Assert.Equal(3 * 5 + 2 * 7, produto.Derivada);
    }

    [Fact]
    public void AConstanteTemDerivadaZeroEAVariavelUm()
    {
        Assert.Equal(0, Dual.Constante(4).Derivada);
        Assert.Equal(1, Dual.Variavel(4).Derivada);
    }

    /// <summary>
    /// Os numeros duais batem com a derivada simbolica ATE O ULTIMO BIT na
    /// maioria dos casos. Nao e sorte: e a mesma conta, nas mesmas operacoes.
    /// </summary>
    [Fact]
    public void ODualBateComASimbolica()
    {
        foreach (var (nome, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var x = new[] { ponto };
            var certo = Simbolica.Derivar(f).Avaliar(x);
            var dual = Avante.Derivada(f, x, 0);
            Assert.True(Math.Abs(dual - certo) <= 1e-14 * Math.Max(1, Math.Abs(certo)), nome);
        }
    }

    [Fact]
    public void OReversoBateComASimbolica()
    {
        foreach (var (nome, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var x = new[] { ponto };
            var certo = Simbolica.Derivar(f).Avaliar(x);
            var reverso = Reversa.Gradiente(f, x)[0];
            Assert.True(Math.Abs(reverso - certo) <= 1e-14 * Math.Max(1, Math.Abs(certo)), nome);
        }
    }

    [Fact]
    public void OsDoisModosDaoOMesmoGradiente()
    {
        var f = Funcoes.Rosenbrock();
        foreach (var ponto in new[] { new[] { 0.5, 2.0 }, [1.0, 1.0], [-1.2, 1.0] })
        {
            var avante = Avante.Gradiente(f, ponto);
            var reverso = Reversa.Gradiente(f, ponto);
            for (var i = 0; i < 2; i++) Assert.Equal(avante[i], reverso[i], 1e-12);
        }
    }

    /// <summary>
    /// A variavel que aparece VARIAS vezes na expressao soma as contribuicoes.
    /// Atribuir em vez de somar no modo reverso daria so a ultima aparicao, e o
    /// erro passaria despercebido em expressao simples.
    /// </summary>
    [Fact]
    public void AVariavelRepetidaSomaAsContribuicoes()
    {
        var x = Expressao.X();
        var f = x * x * x;
        double[] ponto = [2.0];

        Assert.Equal(12, Reversa.Gradiente(f, ponto)[0], 1e-12);
        Assert.Equal(12, Avante.Derivada(f, ponto, 0), 1e-12);
    }

    [Fact]
    public void OGradienteDeMuitasVariaveisBate()
    {
        const int n = 20;
        var f = Funcoes.SomaDeQuadrados(n);
        var ponto = Enumerable.Range(0, n).Select(i => i / 3.0).ToArray();

        var reverso = Reversa.Gradiente(f, ponto);
        for (var i = 0; i < n; i++) Assert.Equal(2 * ponto[i], reverso[i], 1e-12);
    }

    /// <summary>
    /// A fita do modo reverso tem o tamanho da EXPRESSAO, nao do numero de
    /// variaveis. E isso que permite um gradiente de um milhao de entradas numa
    /// passada.
    /// </summary>
    [Fact]
    public void AFitaTemOTamanhoDaExpressao()
    {
        foreach (var variaveis in new[] { 2, 10, 50 })
        {
            var f = Funcoes.SomaDeQuadrados(variaveis);
            var ponto = new double[variaveis];
            Assert.Equal(f.Nos, Reversa.Gravar(f, ponto).Tamanho);
        }
    }

    [Fact]
    public void AFitaGuardaOValorDaFuncao()
    {
        var f = Funcoes.Rosenbrock();
        double[] ponto = [0.5, 2.0];
        Assert.Equal(f.Avaliar(ponto), Reversa.Gravar(f, ponto).Valor, 1e-12);
    }

    /// <summary>
    /// A conta de custo: com uma variavel os dois empatam, e com muitas o
    /// reverso ganha proporcionalmente.
    /// </summary>
    [Fact]
    public void OCustoDoReversoNaoCresceComAsVariaveis()
    {
        var pequena = Reversa.Custo(Funcoes.SomaDeQuadrados(10), 10);
        var grande = Reversa.Custo(Funcoes.SomaDeQuadrados(100), 100);

        Assert.True(pequena.Avante / (double)pequena.Atras > 4);
        Assert.True(grande.Avante / (double)grande.Atras > 40);
    }

    [Fact]
    public void OsOperadoresDoDualSaoConsistentes()
    {
        var a = new Dual(3, 1);
        var b = new Dual(2, 0);

        Assert.Equal(new Dual(5, 1), a + b);
        Assert.Equal(new Dual(1, 1), a - b);
        Assert.Equal(new Dual(-3, -1), -a);
        Assert.Equal(1.5, (a / b).Valor, 1e-12);
        Assert.Equal(0.5, (a / b).Derivada, 1e-12);
    }

    [Fact]
    public void OValorDoDualEhOValorDaFuncao()
    {
        foreach (var (_, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var x = new[] { ponto };
            Assert.Equal(f.Avaliar(x), Avante.Avaliar(f, x, 0).Valor, 1e-12);
        }
    }
}
