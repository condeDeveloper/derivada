namespace Conde.Derivada;

/// <summary>
/// A derivada SIMBOLICA: as regras do caderno, aplicadas na arvore.
///
/// Ela e o juiz deste repositorio, e e um juiz de um tipo raro: ela nao devolve
/// um numero aproximado, ela devolve OUTRA EXPRESSAO, que pode ser avaliada em
/// qualquer ponto com a mesma precisao da original. Quando a derivada automatica
/// e a simbolica discordam num ponto, nao ha margem para "ficou perto".
///
/// E ela tambem e o PROBLEMA que a diferenciacao automatica resolve, so que o
/// problema nao e bem o que eu escrevi primeiro. A regra do produto troca um no
/// por tres e a do quociente poe o denominador DUAS vezes, e dai eu conclui que
/// a expressao cresce de forma exponencial. Medindo, ela cresce ao QUADRADO:
/// com 32 niveis de fracao continua, a funcao tem 129 nos e a derivada 4.449.
///
/// O motivo de nao ser exponencial e que as duas aparicoes do denominador sao O
/// MESMO OBJETO. A estrutura e um grafo, nao uma arvore, e contando nos
/// distintos a derivada tem 353 e nao 4.449. O inchaco exponencial existe e
/// acontece depois: quando a expressao e EXPANDIDA, ao ser impressa ou copiada.
/// </summary>
public static class Simbolica
{
    /// <summary>A derivada da expressao em relacao a variavel dada.</summary>
    public static Expressao Derivar(Expressao f, int variavel = 0)
    {
        ArgumentNullException.ThrowIfNull(f);

        var zero = Expressao.Numero(0);
        var um = Expressao.Numero(1);

        return f.Operacao switch
        {
            Operacao.Constante => zero,
            Operacao.Variavel => f.Variavel == variavel ? um : zero,
            Operacao.Soma => Derivar(f.Esquerda!, variavel) + Derivar(f.Direita!, variavel),
            Operacao.Subtracao => Derivar(f.Esquerda!, variavel) - Derivar(f.Direita!, variavel),

            // A regra do produto: um no vira tres. E aqui que o inchaco nasce.
            Operacao.Produto => Derivar(f.Esquerda!, variavel) * f.Direita!
                                + f.Esquerda! * Derivar(f.Direita!, variavel),

            // E a do quociente vira cinco, e com o denominador aparecendo DUAS
            // vezes. Cada aparicao sera derivada de novo se houver mais um
            // nivel acima.
            Operacao.Divisao => (Derivar(f.Esquerda!, variavel) * f.Direita!
                                 - f.Esquerda! * Derivar(f.Direita!, variavel))
                                / f.Direita!.Elevado(2),

            Operacao.Potencia => Expressao.Numero(f.Expoente) * f.Esquerda!.Elevado(f.Expoente - 1)
                                 * Derivar(f.Esquerda!, variavel),

            Operacao.Seno => Expressao.Cos(f.Esquerda!) * Derivar(f.Esquerda!, variavel),
            Operacao.Cosseno => zero - Expressao.Sen(f.Esquerda!) * Derivar(f.Esquerda!, variavel),
            Operacao.Exponencial => Expressao.Exp(f.Esquerda!) * Derivar(f.Esquerda!, variavel),
            Operacao.Logaritmo => Derivar(f.Esquerda!, variavel) / f.Esquerda!,

            _ => throw new InvalidOperationException("operacao desconhecida"),
        };
    }

    /// <summary>
    /// Simplifica o obvio: soma com zero, produto por um, produto por zero.
    ///
    /// Isso NAO resolve o inchaco, e a medida mostra: ela corta um pedaco grande
    /// das expressoes pequenas e quase nada das grandes, porque o que cresce nas
    /// grandes sao produtos de coisas que nao sao zero nem um. Simplificacao de
    /// verdade e um problema aberto em geral, e e por isso que a resposta
    /// pratica foi outra: nao construir a expressao.
    /// </summary>
    public static Expressao Simplificar(Expressao f)
    {
        ArgumentNullException.ThrowIfNull(f);

        if (f.Esquerda is null && f.Direita is null) return f;

        var esquerda = f.Esquerda is null ? null : Simplificar(f.Esquerda);
        var direita = f.Direita is null ? null : Simplificar(f.Direita);

        var ehZeroEsquerda = EhNumero(esquerda, 0);
        var ehZeroDireita = EhNumero(direita, 0);
        var ehUmEsquerda = EhNumero(esquerda, 1);
        var ehUmDireita = EhNumero(direita, 1);

        switch (f.Operacao)
        {
            case Operacao.Soma:
                if (ehZeroEsquerda) return direita!;
                if (ehZeroDireita) return esquerda!;
                break;
            case Operacao.Subtracao:
                if (ehZeroDireita) return esquerda!;
                break;
            case Operacao.Produto:
                if (ehZeroEsquerda || ehZeroDireita) return Expressao.Numero(0);
                if (ehUmEsquerda) return direita!;
                if (ehUmDireita) return esquerda!;
                break;
            case Operacao.Divisao:
                if (ehZeroEsquerda) return Expressao.Numero(0);
                if (ehUmDireita) return esquerda!;
                break;
            case Operacao.Potencia:
                if (f.Expoente == 0) return Expressao.Numero(1);
                if (f.Expoente == 1) return esquerda!;
                break;
        }

        return Remontar(f, esquerda, direita);
    }

    private static bool EhNumero(Expressao? f, double valor) =>
        f is not null && f.Operacao == Operacao.Constante && Math.Abs(f.Valor - valor) < 1e-15;

    private static Expressao Remontar(Expressao f, Expressao? esquerda, Expressao? direita) =>
        f.Operacao switch
        {
            Operacao.Soma => esquerda! + direita!,
            Operacao.Subtracao => esquerda! - direita!,
            Operacao.Produto => esquerda! * direita!,
            Operacao.Divisao => esquerda! / direita!,
            Operacao.Potencia => esquerda!.Elevado(f.Expoente),
            Operacao.Seno => Expressao.Sen(esquerda!),
            Operacao.Cosseno => Expressao.Cos(esquerda!),
            Operacao.Exponencial => Expressao.Exp(esquerda!),
            Operacao.Logaritmo => Expressao.Log(esquerda!),
            _ => f,
        };

    /// <summary>O gradiente simbolico: uma derivada por variavel.</summary>
    public static Expressao[] Gradiente(Expressao f, int variaveis)
    {
        ArgumentNullException.ThrowIfNull(f);
        return [.. Enumerable.Range(0, variaveis).Select(i => Derivar(f, i))];
    }
}
