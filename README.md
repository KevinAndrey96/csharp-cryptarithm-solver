# Solucionador de criptoaritmética en C# (.NET 10)

[![CI](https://github.com/KevinAndrey96/csharp-cryptarithm-solver/actions/workflows/ci.yml/badge.svg)](https://github.com/KevinAndrey96/csharp-cryptarithm-solver/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10%20LTS-512BD4?logo=dotnet&logoColor=white)
![C# 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)
![xUnit v3](https://img.shields.io/badge/xUnit-v3-5E2B97)
![Cobertura 99 %](https://img.shields.io/badge/cobertura-99%25-brightgreen)
[![Licencia MIT](https://img.shields.io/badge/licencia-MIT-green)](LICENSE)

Solucionador de **criptoaritmética** (alfaméticas) en **C#**: recibe ecuaciones donde cada letra representa un dígito, como `SEND + MORE = MONEY`, y encuentra todas las asignaciones válidas con **búsqueda con retroceso** y **poda por columnas**. Nació para resolver una **división larga** propuesta en clase en 2019 y hoy resuelve cualquier sistema de ecuaciones con suma, resta y multiplicación.

## Contenido

- [El problema de clase](#el-problema-de-clase)
- [Uso](#uso)
- [Algoritmo](#algoritmo)
- [Lenguaje de ecuaciones](#lenguaje-de-ecuaciones)
- [Defectos de la versión de 2019 corregidos](#defectos-de-la-versión-de-2019-corregidos)
- [Arquitectura](#arquitectura)
- [Pruebas y calidad](#pruebas-y-calidad)
- [Historia del proyecto](#historia-del-proyecto)

## El problema de clase

Reconstruir la división larga en la que cada letra es un dígito distinto:

```text
             U X                       9 7
        ----------                ----------
  L M N ) R S T U N        6 0 3 ) 5 8 4 9 3
          R T Y X                  5 4 2 7
          -------                  -------
            T Y Y N                  4 2 2 3
            T Y Y J                  4 2 2 1
            -------                  -------
                  Y                        2
```

Cada paso del algoritmo escrito es una ecuación:

| Paso | Ecuación |
| --- | --- |
| Primer producto parcial | $U \cdot LMN = RTYX$ |
| Primera resta | $RSTU - RTYX = TYY$ |
| Segundo producto parcial | $X \cdot LMN = TYYJ$ |
| Segunda resta | $TYYN - TYYJ = Y$ |
| Prueba de la división | $LMN \cdot UX + Y = RSTUN$ |

La única solución es $603 \cdot 97 + 2 = 58493$, y sigue siendo única aunque se permita repetir dígitos.

## Uso

```bash
dotnet build -c Release
dotnet src/Cryptarithm.Cli/bin/Release/net10.0/cryptarithm-cli.dll resolver division
```

```text
División larga RSTUN ÷ LMN = UX con residuo Y (problema de clase)
  U * LMN = RTYX
  RSTU - RTYX = TYY
  X * LMN = TYYJ
  TYYN - TYYJ = Y
  LMN * UX + Y = RSTUN

Solución 1: J=1 L=6 M=0 N=3 R=5 S=8 T=4 U=9 X=7 Y=2
  9 * 603 = 5427
  5849 - 5427 = 422
  7 * 603 = 4221
  4223 - 4221 = 2
  603 * 97 + 2 = 58493

1 solución, 1.096 asignaciones probadas.
```

| Comando | Efecto |
| --- | --- |
| `listar` | Muestra los acertijos incluidos: `division`, `send-more-money` y `tehe-ahaha` |
| `resolver <nombre>` | Resuelve un acertijo incluido |
| `ecuacion "TO + GO = OUT" [...]` | Resuelve un sistema propio, una ecuación por argumento |
| `--repetir-digitos` | Permite que letras distintas compartan dígito |

Se imprimen las primeras 10 soluciones y se cuentan todas. Códigos de salida: `0` con solución, `1` sin solución y `2` ante un comando o una ecuación inválidos, con la columna exacta del error.

## Algoritmo

**Retroceso.** Se asigna un dígito a una letra a la vez, sin repetir dígitos y sin ceros a la izquierda en palabras de dos o más letras. Si una asignación parcial ya contradice alguna ecuación, se descarta toda esa rama.

**Poda por columnas.** La suma, la resta y la multiplicación conmutan con el módulo:

$$
(a \circ b) \bmod 10^k = \big((a \bmod 10^k) \circ (b \bmod 10^k)\big) \bmod 10^k, \qquad \circ \in \{+, -, \times\}
$$

Por eso, en cuanto las últimas $k$ letras de cada palabra de una ecuación tienen dígito, los últimos $k$ dígitos de ambos lados ya deben coincidir. El solucionador asigna las letras **por columnas, de las unidades hacia la izquierda**, como se resuelven a mano, y verifica cada ecuación al completar cada columna. Así descarta una rama equivocada mucho antes de llegar a las cifras más significativas.

| Acertijo | Espacio de búsqueda | Asignaciones probadas |
| --- | --- | --- |
| División de clase (10 letras) | $7{,}29 \times 10^9$ combinaciones en los ciclos de 2019 | 1.096 |
| `SEND + MORE = MONEY` (8 letras) | $10!/2! = 1.814.400$ asignaciones completas | 7.956 |
| `TEHE + AHAHA = TEHAW` (5 letras) | $30.240$ asignaciones completas | 1.258 |

Los valores se calculan con `Int128` y aritmética verificada, así que un desbordamiento nunca produce una solución falsa.

## Lenguaje de ecuaciones

```text
ecuación := suma '=' suma
suma     := producto (('+' | '-') producto)*
producto := factor ('*' factor)*
factor   := palabra | número | '(' suma ')'
```

Las palabras son letras de la A a la Z (se aceptan minúsculas) de hasta 18 letras, y los números son literales decimales. El analizador es descendente recursivo y reporta la columna del error: `expected '=' at column 12 of "SEND + MORE"`.

## Defectos de la versión de 2019 corregidos

| Defecto original | Corrección |
| --- | --- |
| Diez ciclos `for` anidados recorrían $7{,}29 \times 10^9$ combinaciones | Retroceso con poda por columnas: 1.096 asignaciones |
| No exigía que letras distintas tuvieran dígitos distintos | Dígitos distintos por defecto, `--repetir-digitos` como opción |
| `Console.ReadKey()` detenía el programa en cada solución | Lista de soluciones y conteo final |
| El acertijo estaba escrito en el código; el segundo ejercicio quedó comentado | Analizador de ecuaciones y catálogo con ambos ejercicios |
| Proyecto de .NET Framework con binarios y caché de Visual Studio en el repositorio | SDK de .NET 10, `.gitignore` e historial purgado |

## Arquitectura

| Proyecto | Contenido |
| --- | --- |
| `src/Cryptarithm` | Biblioteca: árbol de expresiones (`Word`, `Number`, `Binary`, `Equation`), `Parser`, `Puzzle`, `Solver` y `Catalog` |
| `src/Cryptarithm.Cli` | Interfaz de línea de comandos en español |
| `tests/Cryptarithm.Tests` | Pruebas xUnit v3 sobre Microsoft Testing Platform |

`Directory.Build.props` activa los tipos de referencia anulables, los analizadores `latest-recommended`, el estilo de código en compilación y `TreatWarningsAsErrors` en todos los proyectos.

## Pruebas y calidad

```bash
dotnet test -c Release --coverage --coverage-output-format cobertura
```

- 48 pruebas con 99 % de cobertura de líneas.
- El solucionador se compara con la enumeración exhaustiva de todas las asignaciones en 60 acertijos aleatorios, con y sin dígitos distintos.
- La evaluación por columnas se compara con la evaluación completa módulo $10^k$ en 500 asignaciones aleatorias.
- Los resultados de la división, `SEND + MORE = MONEY` y sus 155 soluciones con dígitos repetidos se verificaron además con un programa independiente en Python.

GitHub Actions compila y prueba en Linux, Windows y macOS, y ejecuta una prueba de humo de la CLI.

## Historia del proyecto

Ejercicio de mayo de 2019 escrito en Visual Studio con .NET Framework. El código original se conserva en la etiqueta [`v0.1-csharp-coursework-2019`](https://github.com/KevinAndrey96/csharp-cryptarithm-solver/tree/v0.1-csharp-coursework-2019); la caché de Visual Studio y los binarios compilados se purgaron del historial. En 2026 se reescribió como biblioteca con analizador de ecuaciones, poda por columnas, pruebas y CI.

## Autor

**Kevin Andrey Herrera Silva** · [GitHub](https://github.com/KevinAndrey96)

## Licencia

[MIT](LICENSE) © 2019-2026 Kevin Andrey Herrera Silva
