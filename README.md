# Solucionador de Criptoaritmética de División en C# (.NET)

[![.NET](https://img.shields.io/badge/.NET-C%23-purple.svg)](https://dotnet.microsoft.com/)
[![Algorithms](https://img.shields.io/badge/Algorithms-Constraint%20Satisfaction-blue.svg)](https://en.wikipedia.org/wiki/Cryptarithm)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Solucionador algorítmico en **C# (.NET)** para problemas clásicos de **criptoaritmética (alfamética)** basados en esquemas de división aritmética manual larga. El algoritmo implementa un motor de búsqueda y satisfacción de restricciones (CSP) para deducir la asignación biyectiva única de dígitos enteros a letras incógnitas.

---

## 🧩 Fundamento de la Criptoaritmética

La criptoaritmética es un tipo de acertijo matemático donde las operaciones aritméticas clásicas (adición, sustracción, multiplicación o división) tienen sus dígitos del $0$ al $9$ reemplazados por letras del alfabeto.

Las reglas fundamentales que rigen estos sistemas son:
1. **Biyectividad estricta**: Cada letra representa un único dígito decimal ($0 \dots 9$), y dos letras distintas no pueden compartir el mismo valor numérico.
2. **Sin ceros a la izquierda**: Las letras que ocupan la posición más significativa de un número (como $U, Y, X$) deben ser estrictamente distintas de cero ($1 \dots 9$).
3. **Consistencia posicional**: La operación aritmética debe cumplirse con exactitud en base 10 respetando el acarreo y los residuos intermedios de cada etapa de la división.

---

## 🔬 Modelo Aritmético del Problema

El programa resuelve el problema de división planteado en el curso de teoría de la información y criptografía:

```text
       S  T  U
     - R  T  Y  X
     ------------
          T  Y  Y
```

Donde intervienen las variables $U, L, M, N, R, T, Y, X, S, J$, sujetas al sistema de ecuaciones algebraicas posicionales:

1. **Primera Multiplicación Parcial**:
   $$U \times (100L + 10M + N) = 1000R + 100T + 10Y + X$$

2. **Resta y Residuo Intermedio**:
   $$(100S + 10T + U) - (100T + 10Y + X) = 100T + 10Y + Y$$

3. **Segunda Multiplicación Parcial**:
   $$X \times (100L + 10M + N) = 1000T + 100Y + 10Y + J$$

4. **Verificación de Residuos**:
   $$(N - J) = Y$$

El algoritmo evalúa el espacio combinatorio mediante bucles anidados con evaluación en cortocircuito y poda temprana (*pruning*), reduciendo drásticamente las iteraciones necesarias hasta aislar la solución matemática válida.

---

## 📂 Estructura del Proyecto

```text
crypto/
├── Crypto.sln            # Archivo de solucion de Visual Studio
└── Crypto/
    ├── Program.cs        # Punto de entrada con el motor de resolucion algoritmica
    ├── App.config        # Configuracion de ejecucion .NET
    └── Crypto.csproj     # Definicion de proyecto en C#
```

---

## 🚀 Requisitos y Compilación

### Prerrequisitos

- **.NET SDK** (o Visual Studio 2017/2019/2022 / Mono en entornos Unix).

### Compilación y Ejecución con .NET CLI

```bash
# Compilar y ejecutar directamente desde la terminal
dotnet run --project Crypto/Crypto.csproj
```

### Ejecución con Mono (macOS / Linux)

```bash
# Compilar con mcs (Mono C# Compiler)
mcs -out:Crypto.exe Crypto/Program.cs

# Ejecutar el binario generado
mono Crypto.exe
```

---

## 💻 Salida del Algoritmo

Al satisfacer todas las restricciones aritméticas, el programa reporta los valores asignados a cada variable:

```text
Por favor espere...
Solución encontrada:
U: 8, L: 1, M: 2, N: 4, R: 0, T: 9, Y: 9, X: 2, S: 1, J: 5
```

---

## 📄 Licencia

Distribuido bajo la Licencia MIT. Consulta el archivo `LICENSE` para más información.
