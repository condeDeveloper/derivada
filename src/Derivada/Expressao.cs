namespace Conde.Derivada;

public enum Operacao
{
    Constante,
    Variavel,
    Soma,
    Subtracao,
    Produto,
    Divisao,
    Potencia,
    Seno,
    Cosseno,
    Exponencial,
    Logaritmo,
}

/// <summary>
/// Uma EXPRESSAO, em arvore.
///
/// Ela existe para que os quatro jeitos de derivar deste repositorio falem da
/// MESMA funcao. Sem ela, comparar derivada simbolica com numero dual seria
/// comparar duas funcoes escritas duas vezes, e a primeira divergencia seria um
/// erro de transcricao, nao um achado.
///
/// A potencia aceita so expoente inteiro. Nao e preguica: com expoente real a
/// derivada precisa de logaritmo e a funcao deixa de existir para base
/// negativa, e o repositorio inteiro ganharia um caso especial que nao tem nada
/// a ver com o assunto.
/// </summary>
public sealed class Expressao
{
    private Expressao(Operacao operacao, double valor, int variavel, int expoente,
                      Expressao? esquerda, Expressao? direita)
    {
        Operacao = operacao;
        Valor = valor;
        Variavel = variavel;
        Expoente = expoente;
        Esquerda = esquerda;
        Direita = direita;
    }

    public Operacao Operacao { get; }
    public double Valor { get; }
    public int Variavel { get; }
    public int Expoente { get; }
    public Expressao? Esquerda { get; }
    public Expressao? Direita { get; }

    public static Expressao Numero(double valor) =>
        new(Operacao.Constante, valor, 0, 0, null, null);

    public static Expressao X(int indice = 0) =>
        new(Operacao.Variavel, 0, indice, 0, null, null);

    public static Expressao operator +(Expressao a, Expressao b) =>
        new(Operacao.Soma, 0, 0, 0, a, b);

    public static Expressao operator -(Expressao a, Expressao b) =>
        new(Operacao.Subtracao, 0, 0, 0, a, b);

    public static Expressao operator *(Expressao a, Expressao b) =>
        new(Operacao.Produto, 0, 0, 0, a, b);

    public static Expressao operator /(Expressao a, Expressao b) =>
        new(Operacao.Divisao, 0, 0, 0, a, b);

    public Expressao Elevado(int expoente) =>
        new(Operacao.Potencia, 0, 0, expoente, this, null);

    public static Expressao Sen(Expressao a) => new(Operacao.Seno, 0, 0, 0, a, null);
    public static Expressao Cos(Expressao a) => new(Operacao.Cosseno, 0, 0, 0, a, null);
    public static Expressao Exp(Expressao a) => new(Operacao.Exponencial, 0, 0, 0, a, null);
    public static Expressao Log(Expressao a) => new(Operacao.Logaritmo, 0, 0, 0, a, null);

    /// <summary>
    /// Quantos nos a arvore tem. E a medida do INCHACO da derivada simbolica,
    /// que e o motivo de a diferenciacao automatica existir.
    /// </summary>
    public int Nos => 1 + (Esquerda?.Nos ?? 0) + (Direita?.Nos ?? 0);

    /// <summary>
    /// Quantos nos DIFERENTES existem de verdade, contando por identidade.
    ///
    /// Esta contagem me corrigiu. A regra do quociente poe o denominador duas
    /// vezes na derivada, e eu escrevi que isso faz a expressao crescer de forma
    /// exponencial. Ela nao cresce: as duas aparicoes sao O MESMO OBJETO, a
    /// estrutura e um grafo e nao uma arvore, e a memoria cresce devagar.
    ///
    /// O inchaco exponencial e real e acontece em outro momento: quando a
    /// expressao e EXPANDIDA, ao ser impressa, copiada ou simplificada no
    /// braco. Ai cada aparicao vira uma copia de verdade.
    /// </summary>
    public int NosDistintos
    {
        get
        {
            var vistos = new HashSet<Expressao>(ReferenceEqualityComparer.Instance);
            Contar(vistos);
            return vistos.Count;
        }
    }

    private void Contar(HashSet<Expressao> vistos)
    {
        if (!vistos.Add(this)) return;
        Esquerda?.Contar(vistos);
        Direita?.Contar(vistos);
    }

    /// <summary>Quantas variaveis diferentes aparecem.</summary>
    public int Variaveis => Indices().Count;

    private HashSet<int> Indices()
    {
        var saida = new HashSet<int>();
        Juntar(saida);
        return saida;
    }

    private void Juntar(HashSet<int> saida)
    {
        if (Operacao == Operacao.Variavel) saida.Add(Variavel);
        Esquerda?.Juntar(saida);
        Direita?.Juntar(saida);
    }

    /// <summary>O valor da expressao nos pontos dados.</summary>
    public double Avaliar(params double[] x)
    {
        ArgumentNullException.ThrowIfNull(x);
        return Operacao switch
        {
            Operacao.Constante => Valor,
            Operacao.Variavel => x[Variavel],
            Operacao.Soma => Esquerda!.Avaliar(x) + Direita!.Avaliar(x),
            Operacao.Subtracao => Esquerda!.Avaliar(x) - Direita!.Avaliar(x),
            Operacao.Produto => Esquerda!.Avaliar(x) * Direita!.Avaliar(x),
            Operacao.Divisao => Esquerda!.Avaliar(x) / Direita!.Avaliar(x),
            Operacao.Potencia => Math.Pow(Esquerda!.Avaliar(x), Expoente),
            Operacao.Seno => Math.Sin(Esquerda!.Avaliar(x)),
            Operacao.Cosseno => Math.Cos(Esquerda!.Avaliar(x)),
            Operacao.Exponencial => Math.Exp(Esquerda!.Avaliar(x)),
            Operacao.Logaritmo => Math.Log(Esquerda!.Avaliar(x)),
            _ => throw new InvalidOperationException("operacao desconhecida"),
        };
    }

    public override string ToString() => Operacao switch
    {
        Operacao.Constante => Valor.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
        Operacao.Variavel => Variavel == 0 ? "x" : $"x{Variavel}",
        Operacao.Soma => $"({Esquerda} + {Direita})",
        Operacao.Subtracao => $"({Esquerda} - {Direita})",
        Operacao.Produto => $"({Esquerda} * {Direita})",
        Operacao.Divisao => $"({Esquerda} / {Direita})",
        Operacao.Potencia => $"{Esquerda}^{Expoente}",
        Operacao.Seno => $"sen({Esquerda})",
        Operacao.Cosseno => $"cos({Esquerda})",
        Operacao.Exponencial => $"exp({Esquerda})",
        Operacao.Logaritmo => $"log({Esquerda})",
        _ => "?",
    };
}
