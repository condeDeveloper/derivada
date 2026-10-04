namespace Conde.Derivada;

/// <summary>
/// A derivada PARA TRAS: o gradiente inteiro em UMA passada.
///
/// O modo para a frente responde "como a saida muda quando ESTA entrada muda", e
/// precisa de uma passada por entrada. O modo reverso responde "como ESTA saida
/// muda quando cada entrada muda", e precisa de uma passada so, qualquer que
/// seja o numero de entradas.
///
/// A conta e esta: com n entradas e uma saida, o para a frente custa n vezes a
/// funcao e o reverso custa umas tres vezes a funcao, fixo. Com uma rede neural
/// de um milhao de parametros, isso e a diferenca entre existir e nao existir. O
/// nome popular disso e retropropagacao.
///
/// O preco e MEMORIA: a passada para a frente precisa guardar o valor de cada
/// no para a passada de volta usar. O para a frente nao guarda nada.
/// </summary>
public static class Reversa
{
    /// <summary>
    /// Um no da fita: o que foi calculado, e de onde veio.
    ///
    /// A fita e a estrutura que o modo reverso precisa e o direto nao. Ela
    /// cresce com o tamanho da EXPRESSAO, nao com o numero de variaveis, e e
    /// por isso que o modo reverso nao liga de ter um milhao de entradas.
    /// </summary>
    public sealed class No
    {
        public No(Expressao expressao, double valor, int esquerda, int direita)
        {
            Expressao = expressao;
            Valor = valor;
            Esquerda = esquerda;
            Direita = direita;
        }

        public Expressao Expressao { get; }
        public double Valor { get; }
        public int Esquerda { get; }
        public int Direita { get; }
        public double Adjunto { get; set; }
    }

    /// <summary>A fita de uma avaliacao, com os valores guardados.</summary>
    public sealed class Fita
    {
        public List<No> Nos { get; } = [];

        public int Tamanho => Nos.Count;

        /// <summary>O valor da raiz, que e o valor da funcao.</summary>
        public double Valor => Nos[^1].Valor;
    }

    /// <summary>
    /// A passada para a frente: calcula e GUARDA, montando a fita.
    /// </summary>
    public static Fita Gravar(Expressao f, double[] x)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(x);

        var fita = new Fita();
        Montar(f, x, fita);
        return fita;
    }

    private static int Montar(Expressao f, double[] x, Fita fita)
    {
        var esquerda = f.Esquerda is null ? -1 : Montar(f.Esquerda, x, fita);
        var direita = f.Direita is null ? -1 : Montar(f.Direita, x, fita);

        var a = esquerda >= 0 ? fita.Nos[esquerda].Valor : 0;
        var b = direita >= 0 ? fita.Nos[direita].Valor : 0;

        var valor = f.Operacao switch
        {
            Operacao.Constante => f.Valor,
            Operacao.Variavel => x[f.Variavel],
            Operacao.Soma => a + b,
            Operacao.Subtracao => a - b,
            Operacao.Produto => a * b,
            Operacao.Divisao => a / b,
            Operacao.Potencia => Math.Pow(a, f.Expoente),
            Operacao.Seno => Math.Sin(a),
            Operacao.Cosseno => Math.Cos(a),
            Operacao.Exponencial => Math.Exp(a),
            Operacao.Logaritmo => Math.Log(a),
            _ => throw new InvalidOperationException("operacao desconhecida"),
        };

        fita.Nos.Add(new No(f, valor, esquerda, direita));
        return fita.Nos.Count - 1;
    }

    /// <summary>
    /// A passada de volta: do fim para o comeco, espalhando o adjunto.
    ///
    /// O adjunto de um no e "quanto a saida final muda quando este no muda". O
    /// da raiz e um, por definicao, e cada no passa o proprio adjunto para os
    /// filhos multiplicado pela derivada local. Percorrer de tras para frente
    /// garante que, ao chegar num no, todos os que dependem dele ja somaram a
    /// parte deles: e por isso que a fita e percorrida em ordem inversa e nao
    /// por recursao.
    /// </summary>
    public static double[] Gradiente(Expressao f, double[] x)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(x);

        var fita = Gravar(f, x);
        foreach (var no in fita.Nos) no.Adjunto = 0;
        fita.Nos[^1].Adjunto = 1;

        var saida = new double[x.Length];

        for (var i = fita.Tamanho - 1; i >= 0; i--)
        {
            var no = fita.Nos[i];
            var peso = no.Adjunto;
            if (peso == 0 && no.Expressao.Operacao != Operacao.Variavel) continue;

            var a = no.Esquerda >= 0 ? fita.Nos[no.Esquerda].Valor : 0;
            var b = no.Direita >= 0 ? fita.Nos[no.Direita].Valor : 0;

            switch (no.Expressao.Operacao)
            {
                case Operacao.Variavel:
                    // Uma variavel pode aparecer varias vezes na expressao, e
                    // cada aparicao e um no diferente. Somar em vez de atribuir
                    // e o que faz as contribuicoes se juntarem.
                    saida[no.Expressao.Variavel] += peso;
                    break;
                case Operacao.Soma:
                    fita.Nos[no.Esquerda].Adjunto += peso;
                    fita.Nos[no.Direita].Adjunto += peso;
                    break;
                case Operacao.Subtracao:
                    fita.Nos[no.Esquerda].Adjunto += peso;
                    fita.Nos[no.Direita].Adjunto -= peso;
                    break;
                case Operacao.Produto:
                    fita.Nos[no.Esquerda].Adjunto += peso * b;
                    fita.Nos[no.Direita].Adjunto += peso * a;
                    break;
                case Operacao.Divisao:
                    fita.Nos[no.Esquerda].Adjunto += peso / b;
                    fita.Nos[no.Direita].Adjunto -= peso * a / (b * b);
                    break;
                case Operacao.Potencia:
                    fita.Nos[no.Esquerda].Adjunto += peso * no.Expressao.Expoente * Math.Pow(a, no.Expressao.Expoente - 1);
                    break;
                case Operacao.Seno:
                    fita.Nos[no.Esquerda].Adjunto += peso * Math.Cos(a);
                    break;
                case Operacao.Cosseno:
                    fita.Nos[no.Esquerda].Adjunto -= peso * Math.Sin(a);
                    break;
                case Operacao.Exponencial:
                    fita.Nos[no.Esquerda].Adjunto += peso * no.Valor;
                    break;
                case Operacao.Logaritmo:
                    fita.Nos[no.Esquerda].Adjunto += peso / a;
                    break;
            }
        }

        return saida;
    }

    /// <summary>
    /// Quantas avaliacoes de no cada modo gasta para um gradiente completo.
    ///
    /// E a conta que decide qual usar, e ela nao depende de maquina: para a
    /// frente e n vezes o tamanho da expressao, reverso e o tamanho da
    /// expressao mais a volta.
    /// </summary>
    public static (long Avante, long Atras) Custo(Expressao f, int variaveis)
    {
        ArgumentNullException.ThrowIfNull(f);
        return ((long)f.Nos * variaveis, 2L * f.Nos);
    }
}
