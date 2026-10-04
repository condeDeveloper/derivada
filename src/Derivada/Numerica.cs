namespace Conde.Derivada;

/// <summary>
/// As derivadas por DIFERENCA, que todo mundo escreve primeiro.
///
/// Elas tem um passo, e o passo tem um dilema que nao tem saida: grande demais,
/// a formula erra porque a reta nao e a curva; pequeno demais, ela erra porque
/// subtrair dois numeros quase iguais joga fora os digitos significativos. O
/// erro total em funcao do passo e um V, e o fundo do V e o melhor que se
/// consegue.
///
/// Isso nao e defeito de implementacao: e consequencia de calcular derivada
/// SUBTRAINDO. O repositorio mede o V, acha o fundo, compara com a teoria, e
/// depois mostra os dois jeitos de nao ter V nenhum.
/// </summary>
public static class Numerica
{
    /// <summary>
    /// A diferenca PARA A FRENTE: (f(x+h) - f(x)) / h.
    ///
    /// O erro de truncamento e proporcional a h e o de arredondamento a eps/h,
    /// entao o melhor passo e a raiz de eps, uns 1e-8, e o melhor erro possivel
    /// e tambem uns 1e-8. Metade dos digitos vai embora.
    /// </summary>
    public static double Adiante(Expressao f, double[] x, int variavel, double passo)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(x);

        var adiante = (double[])x.Clone();
        adiante[variavel] += passo;
        return (f.Avaliar(adiante) - f.Avaliar(x)) / passo;
    }

    /// <summary>
    /// A diferenca CENTRAL: (f(x+h) - f(x-h)) / (2h).
    ///
    /// Os termos de ordem h se cancelam e o truncamento passa a ser h ao
    /// quadrado. O melhor passo vira a raiz CUBICA de eps, uns 6e-6, e o melhor
    /// erro uns 1e-11. Uma conta a mais compra tres digitos.
    /// </summary>
    public static double Central(Expressao f, double[] x, int variavel, double passo)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(x);

        var adiante = (double[])x.Clone();
        var atras = (double[])x.Clone();
        adiante[variavel] += passo;
        atras[variavel] -= passo;
        return (f.Avaliar(adiante) - f.Avaliar(atras)) / (2 * passo);
    }

    /// <summary>
    /// O PASSO COMPLEXO: anda no eixo imaginario e le a parte imaginaria.
    ///
    /// f(x + ih) = f(x) + ih*f'(x) - h^2/2*f''(x) - ..., entao a parte
    /// imaginaria dividida por h da f'(x) com erro de ordem h ao quadrado. E ate
    /// aqui parece so mais uma formula.
    ///
    /// A diferenca e que NAO HA SUBTRACAO. A parte imaginaria e lida direto, sem
    /// nenhum "quase igual menos quase igual", entao o cancelamento catastrofico
    /// simplesmente nao existe e o passo pode ser absurdamente pequeno. Com
    /// h = 1e-200 o resultado bate ate o ultimo digito.
    ///
    /// O preco e a funcao precisar ser escrita em aritmetica complexa, o que vale
    /// para esta arvore e nao vale para codigo com condicional, valor absoluto ou
    /// biblioteca compilada.
    /// </summary>
    public static double Complexo(Expressao f, double[] x, int variavel, double passo)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(x);

        var ponto = x.Select(v => new Complexa(v, 0)).ToArray();
        ponto[variavel] = new Complexa(x[variavel], passo);
        return Avaliar(f, ponto).Imaginario / passo;
    }

    private static Complexa Avaliar(Expressao f, Complexa[] x) => f.Operacao switch
    {
        Operacao.Constante => new Complexa(f.Valor, 0),
        Operacao.Variavel => x[f.Variavel],
        Operacao.Soma => Avaliar(f.Esquerda!, x) + Avaliar(f.Direita!, x),
        Operacao.Subtracao => Avaliar(f.Esquerda!, x) - Avaliar(f.Direita!, x),
        Operacao.Produto => Avaliar(f.Esquerda!, x) * Avaliar(f.Direita!, x),
        Operacao.Divisao => Avaliar(f.Esquerda!, x) / Avaliar(f.Direita!, x),
        Operacao.Potencia => Complexa.Elevado(Avaliar(f.Esquerda!, x), f.Expoente),
        Operacao.Seno => Complexa.Sen(Avaliar(f.Esquerda!, x)),
        Operacao.Cosseno => Complexa.Cos(Avaliar(f.Esquerda!, x)),
        Operacao.Exponencial => Complexa.Exp(Avaliar(f.Esquerda!, x)),
        Operacao.Logaritmo => Complexa.Log(Avaliar(f.Esquerda!, x)),
        _ => throw new InvalidOperationException("operacao desconhecida"),
    };

    /// <summary>
    /// Varre os passos e devolve o menor erro e o passo que o deu. E a medida do
    /// fundo do V.
    /// </summary>
    public static (double Passo, double Erro) MelhorPasso(
        Func<Expressao, double[], int, double, double> metodo, Expressao f, double[] x, int variavel, double certo)
    {
        ArgumentNullException.ThrowIfNull(metodo);

        double melhorPasso = 1, melhorErro = double.MaxValue;
        for (var potencia = 0; potencia >= -18; potencia--)
        {
            var passo = Math.Pow(10, potencia);
            var erro = Math.Abs(metodo(f, x, variavel, passo) - certo);
            if (erro >= melhorErro) continue;
            melhorErro = erro;
            melhorPasso = passo;
        }
        return (melhorPasso, melhorErro);
    }
}

/// <summary>
/// Um complexo minimo, so com o que o passo complexo precisa.
///
/// Ele e um tipo separado do dual de proposito, e a diferenca entre os dois e o
/// assunto: no dual, e*e = 0; no complexo, i*i = -1. O termo de segunda ordem
/// SOME no dual e so fica pequeno no complexo, e e dai que vem o erro residual
/// de ordem h ao quadrado do passo complexo.
/// </summary>
public readonly struct Complexa
{
    public Complexa(double real, double imaginario)
    {
        Real = real;
        Imaginario = imaginario;
    }

    public double Real { get; }
    public double Imaginario { get; }

    public static Complexa operator +(Complexa a, Complexa b) =>
        new(a.Real + b.Real, a.Imaginario + b.Imaginario);

    public static Complexa operator -(Complexa a, Complexa b) =>
        new(a.Real - b.Real, a.Imaginario - b.Imaginario);

    public static Complexa operator *(Complexa a, Complexa b) =>
        new(a.Real * b.Real - a.Imaginario * b.Imaginario, a.Real * b.Imaginario + a.Imaginario * b.Real);

    public static Complexa operator /(Complexa a, Complexa b)
    {
        var baixo = b.Real * b.Real + b.Imaginario * b.Imaginario;
        return new Complexa((a.Real * b.Real + a.Imaginario * b.Imaginario) / baixo,
                            (a.Imaginario * b.Real - a.Real * b.Imaginario) / baixo);
    }

    public static Complexa Elevado(Complexa a, int expoente)
    {
        var saida = new Complexa(1, 0);
        var negativo = expoente < 0;
        for (var i = 0; i < Math.Abs(expoente); i++) saida *= a;
        return negativo ? new Complexa(1, 0) / saida : saida;
    }

    public static Complexa Sen(Complexa a) =>
        new(Math.Sin(a.Real) * Math.Cosh(a.Imaginario), Math.Cos(a.Real) * Math.Sinh(a.Imaginario));

    public static Complexa Cos(Complexa a) =>
        new(Math.Cos(a.Real) * Math.Cosh(a.Imaginario), -Math.Sin(a.Real) * Math.Sinh(a.Imaginario));

    public static Complexa Exp(Complexa a)
    {
        var modulo = Math.Exp(a.Real);
        return new Complexa(modulo * Math.Cos(a.Imaginario), modulo * Math.Sin(a.Imaginario));
    }

    public static Complexa Log(Complexa a) =>
        new(Math.Log(Math.Sqrt(a.Real * a.Real + a.Imaginario * a.Imaginario)),
            Math.Atan2(a.Imaginario, a.Real));

    public override string ToString() => $"{Real}+{Imaginario}i";
}
