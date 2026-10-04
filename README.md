# derivada

Quatro maneiras de derivar, do zero em C# e .NET 8: simbólica, números duais,
modo reverso e diferenças finitas, todas lendo a **mesma** árvore de expressão. O
juiz é a derivada simbólica, que não devolve um número aproximado e sim outra
expressão.

```
$ dotnet medidor.dll tabela

funcao                            dual       reverso      complexo       central
x^3 - 2x + 1                   0.0E+000      0.0E+000      8.9E-016      1.4E-011
sen(x) * exp(x)                0.0E+000      0.0E+000      4.4E-016      5.9E-011
sen(sen(sen(x)))               0.0E+000      1.1E-016      0.0E+000      1.4E-011
exp(x) - 1                     0.0E+000      0.0E+000      0.0E+000      1.8E-011
produto de 5 fatores           0.0E+000      0.0E+000      0.0E+000      2.5E-009
```

Os números duais e o modo reverso batem com a simbólica **até o último bit**. Não
é sorte: eles fazem a mesma conta, nas mesmas operações e na mesma ordem. A
diferença entre eles é a maneira de organizar a conta, não a conta.

## O dilema do passo

Derivada por diferença tem um passo, e o passo tem um dilema sem saída: grande
demais, a fórmula erra porque a reta não é a curva; pequeno demais, ela erra
porque subtrair dois números quase iguais joga fora os dígitos significativos.

```
$ dotnet medidor.dll passo

     passo         adiante         central        complexo
      1e-2       1.54E-002       8.10E-006       8.10E-006
      1e-6       1.54E-006       3.54E-011       8.08E-014
      1e-8       3.25E-008       1.02E-008       0.00E+000
     1e-12       9.87E-006       9.87E-006       4.44E-016
     1e-16       6.17E-001       4.93E-001       0.00E+000
    1e-200       2.84E+000       2.84E+000       4.44E-016

  o fundo do V da diferenca adiante: passo 1E-008, erro 3.25E-008
  a teoria diz raiz de eps:          passo 1E-008

  o fundo do V da diferenca central: passo 1E-005, erro 2.11E-012
  a teoria diz raiz cubica de eps:   passo 6E-006
```

O fundo do V cai exatamente onde a teoria diz. E a terceira coluna não tem V: o
**passo complexo** anda no eixo imaginário e lê a parte imaginária direto, sem
nenhuma subtração, então o cancelamento catastrófico não existe e o passo pode
ser 1e-200. No passo em que a diferença adiante já não devolve nada, ele está
exato.

## O que as medidas me corrigiram

**O inchaço simbólico é quadrático, não exponencial.** A regra do quociente põe o
denominador **duas vezes** na derivada, e cada cópia é derivada de novo no nível
de cima, então eu concluí que a expressão explode.

```
$ dotnet medidor.dll inchaco

  niveis    f arvore    d arvore   razao    f grafo    d grafo   razao
       1           5          16     3.2          4         12     3.0
       8          33         345    10.5         25         89     3.6
      16          65       1,201    18.5         49        177     3.6
      32         129       4,449    34.5         97        353     3.6
```

Ela cresce ao quadrado, e as duas últimas colunas explicam: as duas aparições do
denominador são **o mesmo objeto**. A estrutura é um grafo, não uma árvore, e
contando nós distintos a derivada cresce linearmente. O inchaço exponencial
existe e acontece depois, quando a expressão é **expandida**, ao ser impressa ou
copiada.

**O cancelamento da função não contamina a derivada.** `exp(x) - 1` perto de zero
é o exemplo clássico de subtração catastrófica, e em x = 1e-16 ela devolve
exatamente zero: todos os dígitos foram embora.

```
$ dotnet medidor.dll cancelamento

         x                f(x)         central            dual        complexo
       1e0       1.718282E+000        5.9E-011        0.0E+000        0.0E+000
     1e-12       1.000089E-012        1.7E-011        0.0E+000        0.0E+000
     1e-16       0.000000E+000        1.8E-011        0.0E+000        0.0E+000
```

Os três métodos acertam a derivada mesmo assim, e eu esperava o contrário. O
motivo é simples quando se olha: a derivada de `exp(x) - 1` é `exp(x)`, que não
tem subtração nenhuma. A derivada é outra conta.

**A simplificação óbvia não resolve o inchaço.** Ela corta 53% de uma derivada
pequena e quase nada de uma grande, porque o que cresce nas grandes são produtos
de coisas que não são zero nem um. Simplificação de verdade é um problema aberto
em geral, e foi por isso que a resposta prática virou outra: **não construir a
expressão**.

## Por que o modo reverso existe

```
$ dotnet medidor.dll gradiente

   variaveis   nos da funcao        avante       reverso     razao
           1               2             2             4       0.5
           2               5            10            10       1.0
          10              29           290            58       5.0
         100             299        29,900           598      50.0
        1000           2,999     2,999,000         5,998     500.0
```

O modo direto responde "como a saída muda quando **esta** entrada muda", e precisa
de uma passada por entrada. O reverso responde "como **esta** saída muda quando
cada entrada muda", e precisa de uma passada só.

Com uma variável os dois empatam, e com mil o reverso é quinhentas vezes mais
barato. Esse número é a razão de a retropropagação existir: uma rede neural é uma
função de milhões de entradas para **um** número, que é a perda. O preço é
memória: a passada para a frente precisa guardar o valor de cada nó para a volta
usar, e o modo direto não guarda nada.

## Dual e complexo não são a mesma ideia

Os dois acrescentam um símbolo e carregam a derivada junto, e a diferença entre
eles é uma linha: no dual, `e*e = 0`; no complexo, `i*i = -1`. O termo de segunda
ordem **some** no dual e só fica pequeno no complexo, e é daí que vem o erro
residual do passo complexo. O dual é exato; o complexo é exato o bastante.

E no dual a regra do produto **sai sozinha**: `(a + be)(c + de) = ac + (ad+bc)e`,
porque o termo com `e*e` desaparece. O coeficiente de `e` saiu obedecendo à regra
do produto sem ninguém ter pedido, e a mesma coisa vale para a regra da cadeia.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Expressao.cs` | a árvore, para os quatro métodos falarem da mesma função |
| `Simbolica.cs` | as regras do caderno, e o juiz |
| `Dual.cs` | o número dual e o modo direto |
| `Reversa.cs` | a fita e o gradiente inteiro numa passada |
| `Numerica.cs` | diferença adiante, central e o passo complexo |
| `Funcoes.cs` | polinômio, Rosenbrock, cadeia de senos e fração contínua |

## Como rodar

```
dotnet test testes/Derivada.Testes/Derivada.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `passo`, `tabela`, `inchaco`, `gradiente`, `cancelamento` e
`tudo`.

## Licença

MIT.
