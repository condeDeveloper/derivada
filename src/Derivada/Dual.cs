namespace Conde.Derivada;

/// <summary>
/// Um NUMERO DUAL: um valor e uma derivada, carregados juntos.
///
/// A ideia e de Clifford, 1873, e cabe em uma linha: acrescente um simbolo e com
/// e*e = 0, e escreva os numeros como a + b*e. Ao multiplicar,
/// (a + b*e)(c + d*e) = ac + (ad + bc)*e, porque o termo com e*e some. O
/// coeficiente de e saiu obedecendo a REGRA DO PRODUTO, sem ninguem ter pedido.
///
/// E isso vale para tudo. O coeficiente de e sempre obedece a regra da cadeia,
/// porque a regra da cadeia e o que a derivada de uma composicao faz. Entao
/// basta escrever a funcao normalmente, com estes numeros no lugar dos de
/// sempre, e a derivada sai junto do valor.
///
/// Nao ha passo, nao ha limite, nao ha aproximacao. O resultado e tao exato
/// quanto a propria avaliacao da funcao.
/// </summary>
public readonly struct Dual : IEquatable<Dual>
{
    public Dual(double valor, double derivada)
    {
        Valor = valor;
        Derivada = derivada;
    }

    public double Valor { get; }
    public double Derivada { get; }

    /// <summary>Uma constante: derivada zero.</summary>
    public static Dual Constante(double valor) => new(valor, 0);

    /// <summary>A variavel em relacao a qual se deriva: derivada um.</summary>
    public static Dual Variavel(double valor) => new(valor, 1);

    public static Dual operator +(Dual a, Dual b) => new(a.Valor + b.Valor, a.Derivada + b.Derivada);

    public static Dual operator -(Dual a, Dual b) => new(a.Valor - b.Valor, a.Derivada - b.Derivada);

    public static Dual operator -(Dual a) => new(-a.Valor, -a.Derivada);

    /// <summary>A regra do produto, que sai do e*e = 0 sem ser escrita.</summary>
    public static Dual operator *(Dual a, Dual b) =>
        new(a.Valor * b.Valor, a.Derivada * b.Valor + a.Valor * b.Derivada);

    public static Dual operator /(Dual a, Dual b) =>
        new(a.Valor / b.Valor, (a.Derivada * b.Valor - a.Valor * b.Derivada) / (b.Valor * b.Valor));

    public static Dual Elevado(Dual a, int expoente) =>
        new(Math.Pow(a.Valor, expoente), expoente * Math.Pow(a.Valor, expoente - 1) * a.Derivada);

    public static Dual Sen(Dual a) => new(Math.Sin(a.Valor), Math.Cos(a.Valor) * a.Derivada);

    public static Dual Cos(Dual a) => new(Math.Cos(a.Valor), -Math.Sin(a.Valor) * a.Derivada);

    public static Dual Exp(Dual a)
    {
        var valor = Math.Exp(a.Valor);
        return new Dual(valor, valor * a.Derivada);
    }

    public static Dual Log(Dual a) => new(Math.Log(a.Valor), a.Derivada / a.Valor);

    public bool Equals(Dual outro) => Valor.Equals(outro.Valor) && Derivada.Equals(outro.Derivada);

    public override bool Equals(object? obj) => obj is Dual outro && Equals(outro);

    public override int GetHashCode() => HashCode.Combine(Valor, Derivada);

    public static bool operator ==(Dual a, Dual b) => a.Equals(b);

    public static bool operator !=(Dual a, Dual b) => !a.Equals(b);

    public override string ToString() =>
        $"{Valor.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)} " +
        $"+ {Derivada.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}e";
}

/// <summary>
/// A derivada PARA A FRENTE: avalia a expressao com numeros duais.
///
/// Ela e exata e custa uma passada por variavel. Com uma variavel so, e a
/// melhor coisa que existe; com mil, sao mil passadas, e e por isso que o modo
/// reverso existe.
/// </summary>
public static class Avante
{
    /// <summary>
    /// O valor e a derivada da expressao em relacao a uma variavel, num ponto.
    /// </summary>
    public static Dual Avaliar(Expressao f, double[] x, int variavel)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(x);

        return f.Operacao switch
        {
            Operacao.Constante => Dual.Constante(f.Valor),
            Operacao.Variavel => f.Variavel == variavel ? Dual.Variavel(x[f.Variavel]) : Dual.Constante(x[f.Variavel]),
            Operacao.Soma => Avaliar(f.Esquerda!, x, variavel) + Avaliar(f.Direita!, x, variavel),
            Operacao.Subtracao => Avaliar(f.Esquerda!, x, variavel) - Avaliar(f.Direita!, x, variavel),
            Operacao.Produto => Avaliar(f.Esquerda!, x, variavel) * Avaliar(f.Direita!, x, variavel),
            Operacao.Divisao => Avaliar(f.Esquerda!, x, variavel) / Avaliar(f.Direita!, x, variavel),
            Operacao.Potencia => Dual.Elevado(Avaliar(f.Esquerda!, x, variavel), f.Expoente),
            Operacao.Seno => Dual.Sen(Avaliar(f.Esquerda!, x, variavel)),
            Operacao.Cosseno => Dual.Cos(Avaliar(f.Esquerda!, x, variavel)),
            Operacao.Exponencial => Dual.Exp(Avaliar(f.Esquerda!, x, variavel)),
            Operacao.Logaritmo => Dual.Log(Avaliar(f.Esquerda!, x, variavel)),
            _ => throw new InvalidOperationException("operacao desconhecida"),
        };
    }

    /// <summary>A derivada parcial, so o numero.</summary>
    public static double Derivada(Expressao f, double[] x, int variavel) => Avaliar(f, x, variavel).Derivada;

    /// <summary>
    /// O gradiente, uma passada por variavel.
    ///
    /// E aqui que o modo para a frente perde: com n variaveis, a funcao e
    /// avaliada n vezes inteiras. O custo e n vezes o custo da funcao.
    /// </summary>
    public static double[] Gradiente(Expressao f, double[] x)
    {
        ArgumentNullException.ThrowIfNull(x);
        var saida = new double[x.Length];
        for (var i = 0; i < x.Length; i++) saida[i] = Derivada(f, x, i);
        return saida;
    }
}
