using Conde.Derivada;

namespace Conde.Derivada.Medidor;

/// <summary>
/// As medidas. Erro medido contra a derivada simbolica e contagem de nos:
/// nenhuma depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "passo": Passo(); break;
            case "tabela": Tabela(); break;
            case "inchaco": Inchaco(); break;
            case "gradiente": Gradiente(); break;
            case "cancelamento": Cancelamento(); break;
            case "tudo": Passo(); Tabela(); Inchaco(); Gradiente(); Cancelamento(); break;
            default:
                Console.Error.WriteLine("medidas: passo, tabela, inchaco, gradiente, cancelamento, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>O V do erro em funcao do passo, que e o dilema sem saida.</summary>
    private static void Passo()
    {
        Console.WriteLine("== o V do erro: truncamento de um lado, arredondamento do outro ==");

        var f = Funcoes.SenoVezesExponencial();
        var x = new[] { 0.7 };
        var certo = Simbolica.Derivar(f).Avaliar(x);

        Console.WriteLine($"  a derivada certa em x = 0,7: {certo:G17}");
        Console.WriteLine();
        Console.WriteLine($"{"passo",10}{"adiante",16}{"central",16}{"complexo",16}");

        foreach (var potencia in new[] { -1, -2, -4, -6, -8, -10, -12, -14, -16, -20, -40, -100, -200 })
        {
            var passo = Math.Pow(10, potencia);
            var adiante = Math.Abs(Numerica.Adiante(f, x, 0, passo) - certo);
            var central = Math.Abs(Numerica.Central(f, x, 0, passo) - certo);
            var complexo = Math.Abs(Numerica.Complexo(f, x, 0, passo) - certo);

            Console.WriteLine($"{"1e" + potencia,10}{adiante,16:E2}{central,16:E2}{complexo,16:E2}");
        }

        Console.WriteLine();
        Console.WriteLine("as duas primeiras colunas descem, chegam ao fundo e SOBEM de novo. Passo");
        Console.WriteLine("grande erra porque a reta nao e a curva; passo pequeno erra porque");
        Console.WriteLine("subtrair dois numeros quase iguais joga fora os digitos significativos");
        Console.WriteLine();

        var fundoAdiante = Numerica.MelhorPasso(Numerica.Adiante, f, x, 0, certo);
        var fundoCentral = Numerica.MelhorPasso(Numerica.Central, f, x, 0, certo);

        Console.WriteLine($"  o fundo do V da diferenca adiante: passo {fundoAdiante.Passo:E0}, erro {fundoAdiante.Erro:E2}");
        Console.WriteLine($"  a teoria diz raiz de eps:          passo {Math.Sqrt(2.22e-16):E0}");
        Console.WriteLine();
        Console.WriteLine($"  o fundo do V da diferenca central: passo {fundoCentral.Passo:E0}, erro {fundoCentral.Erro:E2}");
        Console.WriteLine($"  a teoria diz raiz cubica de eps:   passo {Math.Cbrt(2.22e-16):E0}");
        Console.WriteLine();
        Console.WriteLine("a terceira coluna nao tem V. O passo complexo nao SUBTRAI nada: ele le a");
        Console.WriteLine("parte imaginaria direto, entao o cancelamento catastrofico nao existe e o");
        Console.WriteLine("passo pode ser 1e-200. Com ele, a derivada sai com a precisao da propria");
        Console.WriteLine("avaliacao da funcao");
        Console.WriteLine();
    }

    /// <summary>Os quatro metodos em todas as funcoes de teste.</summary>
    private static void Tabela()
    {
        Console.WriteLine("== os quatro metodos, contra a derivada simbolica ==");
        Console.WriteLine($"{"funcao",-24}{"dual",14}{"reverso",14}{"complexo",14}{"central",14}");

        foreach (var (nome, f, ponto) in Funcoes.DeUmaVariavel())
        {
            var x = new[] { ponto };
            var certo = Simbolica.Derivar(f).Avaliar(x);

            var dual = Math.Abs(Avante.Derivada(f, x, 0) - certo);
            var reverso = Math.Abs(Reversa.Gradiente(f, x)[0] - certo);
            var complexo = Math.Abs(Numerica.Complexo(f, x, 0, 1e-200) - certo);
            var central = Math.Abs(Numerica.Central(f, x, 0, 1e-5) - certo);

            Console.WriteLine($"{nome,-24}{dual,14:E1}{reverso,14:E1}{complexo,14:E1}{central,14:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("os numeros duais e o modo reverso batem com a simbolica ATE O ULTIMO BIT,");
        Console.WriteLine("na maioria das linhas. Nao e coincidencia nem sorte: eles calculam a mesma");
        Console.WriteLine("conta, nas mesmas operacoes e na mesma ordem. A diferenca entre eles e a");
        Console.WriteLine("maneira de organizar a conta, nao a conta");
        Console.WriteLine();
        Console.WriteLine("a diferenca central, com o melhor passo que ela tem, fica dez mil vezes");
        Console.WriteLine("atras. E ela e o metodo que todo mundo escreve primeiro");
        Console.WriteLine();
    }

    /// <summary>O inchaco simbolico, medido de dois jeitos.</summary>
    private static void Inchaco()
    {
        Console.WriteLine("== o inchaco da derivada simbolica: arvore contra grafo ==");
        Console.WriteLine($"{"niveis",8}{"f arvore",12}{"d arvore",12}{"razao",8}{"f grafo",11}{"d grafo",11}{"razao",8}");

        foreach (var niveis in new[] { 1, 2, 4, 8, 16, 32 })
        {
            var f = Funcoes.Fracao(niveis);
            var d = Simbolica.Derivar(f);
            Console.WriteLine($"{niveis,8}{f.Nos,12:N0}{d.Nos,12:N0}{d.Nos / (double)f.Nos,8:N1}" +
                              $"{f.NosDistintos,11:N0}{d.NosDistintos,11:N0}{d.NosDistintos / (double)f.NosDistintos,8:N1}");
        }

        Console.WriteLine();
        Console.WriteLine("eu escrevi que o inchaco era EXPONENCIAL, porque a regra do quociente poe o");
        Console.WriteLine("denominador duas vezes na derivada e cada copia e derivada de novo no nivel");
        Console.WriteLine("de cima. Medindo, ele e QUADRATICO: 129 nos de funcao viram 4.449");
        Console.WriteLine();
        Console.WriteLine("e as duas ultimas colunas explicam: as duas aparicoes do denominador sao O");
        Console.WriteLine("MESMO OBJETO. A estrutura e um grafo, nao uma arvore, e contando nos");
        Console.WriteLine("distintos a derivada cresce LINEARMENTE. O inchaco exponencial e real e");
        Console.WriteLine("acontece quando a expressao e expandida, ao ser impressa ou copiada");
        Console.WriteLine();

        Console.WriteLine($"  {"fatores",10}{"derivada",12}{"simplificada",16}{"cortou",10}");
        foreach (var fatores in new[] { 2, 4, 8, 16 })
        {
            var d = Simbolica.Derivar(Funcoes.Produto(fatores));
            var s = Simbolica.Simplificar(d);
            Console.WriteLine($"  {fatores,10}{d.Nos,12:N0}{s.Nos,16:N0}{1 - s.Nos / (double)d.Nos,10:P0}");
        }

        Console.WriteLine();
        Console.WriteLine("  a simplificacao obvia corta um pedaco grande das expressoes pequenas e");
        Console.WriteLine("  quase nada das grandes, porque o que cresce nas grandes sao produtos de");
        Console.WriteLine("  coisas que nao sao zero nem um");
        Console.WriteLine();
    }

    /// <summary>Modo direto contra modo reverso, em custo.</summary>
    private static void Gradiente()
    {
        Console.WriteLine("== gradiente: uma passada por variavel contra uma passada so ==");
        Console.WriteLine($"{"variaveis",12}{"nos da funcao",16}{"avante",14}{"reverso",14}{"razao",10}");

        foreach (var variaveis in new[] { 1, 2, 10, 100, 1000 })
        {
            var f = Funcoes.SomaDeQuadrados(variaveis);
            var (avante, atras) = Reversa.Custo(f, variaveis);
            Console.WriteLine($"{variaveis,12}{f.Nos,16:N0}{avante,14:N0}{atras,14:N0}{avante / (double)atras,10:N1}");
        }

        Console.WriteLine();
        Console.WriteLine("com uma variavel os dois empatam, e com mil o reverso e quinhentas vezes");
        Console.WriteLine("mais barato. Esse numero e a razao de a retropropagacao existir: uma rede");
        Console.WriteLine("neural e uma funcao de milhoes de entradas para UM numero, que e a perda");
        Console.WriteLine();
        Console.WriteLine("o preco do reverso e MEMORIA. A passada para a frente precisa guardar o");
        Console.WriteLine("valor de cada no para a volta usar, e o modo direto nao guarda nada");
        Console.WriteLine();

        var rosenbrock = Funcoes.Rosenbrock();
        var ponto = new[] { 0.5, 2.0 };
        Console.WriteLine($"  Rosenbrock em (0,5 ; 2), pelos tres caminhos:");
        Console.WriteLine($"    simbolica: [{string.Join(", ", Simbolica.Gradiente(rosenbrock, 2).Select(g => g.Avaliar(ponto)))}]");
        Console.WriteLine($"    avante:    [{string.Join(", ", Avante.Gradiente(rosenbrock, ponto))}]");
        Console.WriteLine($"    reverso:   [{string.Join(", ", Reversa.Gradiente(rosenbrock, ponto))}]");
        Console.WriteLine();
    }

    /// <summary>
    /// O cancelamento que acontece ANTES de qualquer derivada, na propria
    /// funcao.
    /// </summary>
    private static void Cancelamento()
    {
        Console.WriteLine("== quando a propria funcao ja perdeu os digitos ==");

        var f = Funcoes.QuaseZero();
        Console.WriteLine($"  a funcao e exp(x) - 1, e a derivada dela e exp(x)");
        Console.WriteLine();
        Console.WriteLine($"{"x",10}{"f(x)",20}{"central",16}{"dual",16}{"complexo",16}");

        foreach (var potencia in new[] { 0, -4, -8, -12, -16 })
        {
            var ponto = Math.Pow(10, potencia);
            var x = new[] { ponto };
            var certo = Math.Exp(ponto);

            var central = Math.Abs(Numerica.Central(f, x, 0, 1e-5) - certo);
            var dual = Math.Abs(Avante.Derivada(f, x, 0) - certo);
            var complexo = Math.Abs(Numerica.Complexo(f, x, 0, 1e-200) - certo);

            Console.WriteLine($"{"1e" + potencia,10}{f.Avaliar(x),20:E6}{central,16:E1}{dual,16:E1}{complexo,16:E1}");
        }

        Console.WriteLine();
        Console.WriteLine("aqui a derivada e facil e o VALOR e que e dificil: exp(1e-16) - 1 subtrai");
        Console.WriteLine("dois numeros que diferem na decima sexta casa, e o resultado nao tem digito");
        Console.WriteLine("significativo nenhum");
        Console.WriteLine();
        Console.WriteLine("repare que os tres metodos acertam a DERIVADA mesmo assim, e isso e o");
        Console.WriteLine("ponto: a derivada de exp(x) - 1 e exp(x), que nao tem subtracao nenhuma. O");
        Console.WriteLine("cancelamento da funcao nao contamina a derivada, porque a derivada e outra");
        Console.WriteLine("conta. Era o contrario que eu esperava");
        Console.WriteLine();
    }
}
