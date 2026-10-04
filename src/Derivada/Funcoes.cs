namespace Conde.Derivada;

/// <summary>
/// As funcoes de teste, escritas uma vez e derivadas pelos quatro caminhos.
/// </summary>
public static class Funcoes
{
    /// <summary>x^3 - 2x + 1, cuja derivada da para fazer de cabeca: 3x^2 - 2.</summary>
    public static Expressao Polinomio()
    {
        var x = Expressao.X();
        return x.Elevado(3) - Expressao.Numero(2) * x + Expressao.Numero(1);
    }

    /// <summary>sen(x) * exp(x), que junta regra do produto e regra da cadeia.</summary>
    public static Expressao SenoVezesExponencial() => Expressao.Sen(Expressao.X()) * Expressao.Exp(Expressao.X());

    /// <summary>
    /// A funcao de Rosenbrock, de 1960: (1-x)^2 + 100*(y - x^2)^2.
    ///
    /// Ela e o teste classico de otimizacao porque o minimo fica no fundo de um
    /// vale curvo e estreito, onde o gradiente aponta para a parede e quase
    /// nunca para o minimo. Aqui ela serve por outro motivo: tem duas variaveis,
    /// entao da para comparar modo direto com modo reverso.
    /// </summary>
    public static Expressao Rosenbrock()
    {
        var x = Expressao.X(0);
        var y = Expressao.X(1);
        return (Expressao.Numero(1) - x).Elevado(2)
               + Expressao.Numero(100) * (y - x.Elevado(2)).Elevado(2);
    }

    /// <summary>
    /// Um produto de n fatores (x + k), que e a receita do INCHACO simbolico.
    ///
    /// Cada aplicacao da regra do produto troca um no por tres, e com os fatores
    /// aninhados o tamanho da derivada cresce muito mais rapido que o da
    /// funcao. A medida mostra o numero.
    /// </summary>
    public static Expressao Produto(int fatores)
    {
        var x = Expressao.X();
        var saida = x + Expressao.Numero(1);
        for (var k = 2; k <= fatores; k++) saida *= x + Expressao.Numero(k);
        return saida;
    }

    /// <summary>
    /// Uma soma de n variaveis ao quadrado, para medir o custo do gradiente.
    ///
    /// A derivada em relacao a cada variavel e trivial, e e justamente por isso
    /// que ela serve: a diferenca entre os dois modos aparece limpa, sem a
    /// funcao atrapalhar.
    /// </summary>
    public static Expressao SomaDeQuadrados(int variaveis)
    {
        var saida = Expressao.X(0).Elevado(2);
        for (var i = 1; i < variaveis; i++) saida += Expressao.X(i).Elevado(2);
        return saida;
    }

    /// <summary>
    /// Uma cadeia funda: sen(sen(sen(...))), para a regra da cadeia trabalhar.
    /// </summary>
    public static Expressao Cadeia(int fundura)
    {
        var saida = Expressao.X();
        for (var i = 0; i < fundura; i++) saida = Expressao.Sen(saida);
        return saida;
    }

    /// <summary>
    /// Uma fracao continua de n niveis: x / (1 + x / (1 + ...)).
    ///
    /// Esta e a receita do inchaco de VERDADE. A regra do quociente poe o
    /// denominador DUAS vezes na derivada, e cada uma dessas copias sera
    /// derivada de novo pelo nivel de cima. O produto, que cresce ao quadrado,
    /// e inofensivo perto disto.
    /// </summary>
    public static Expressao Fracao(int niveis)
    {
        var x = Expressao.X();
        var saida = x;
        for (var i = 0; i < niveis; i++) saida = x / (Expressao.Numero(1) + saida);
        return saida;
    }

    /// <summary>
    /// Uma funcao com SUBTRACAO QUASE EXATA: exp(x) - 1 perto de zero.
    ///
    /// Ela existe para mostrar que o problema da diferenca finita nao e so do
    /// metodo: aqui a propria funcao ja perde digitos antes de qualquer derivada
    /// ser calculada, e nenhum metodo recupera o que a avaliacao jogou fora.
    /// </summary>
    public static Expressao QuaseZero() =>
        Expressao.Exp(Expressao.X()) - Expressao.Numero(1);

    /// <summary>As funcoes de uma variavel, com nome, para as varreduras.</summary>
    public static IEnumerable<(string Nome, Expressao F, double Ponto)> DeUmaVariavel()
    {
        yield return ("x^3 - 2x + 1", Polinomio(), 1.5);
        yield return ("sen(x) * exp(x)", SenoVezesExponencial(), 0.7);
        yield return ("sen(sen(sen(x)))", Cadeia(3), 0.4);
        yield return ("exp(x) - 1", QuaseZero(), 1e-6);
        yield return ("produto de 5 fatores", Produto(5), 0.3);
    }
}
