namespace Cryptarithm;

/// <summary>Arithmetic expression whose words stand for numbers written with letter digits.</summary>
public abstract record Expression
{
    /// <summary>Evaluates the expression.</summary>
    /// <param name="digits">Digit assigned to each letter, indexed by <c>letter - 'A'</c>.</param>
    /// <returns>The value, computed with overflow checks.</returns>
    public abstract Int128 Evaluate(ReadOnlySpan<int> digits);

    /// <summary>
    /// Evaluates the expression modulo <c>10^columns</c>. Addition, subtraction and multiplication
    /// commute with the modulo, so only the last <paramref name="columns"/> letters of each word
    /// need a digit.
    /// </summary>
    /// <param name="digits">Digit assigned to each letter, indexed by <c>letter - 'A'</c>.</param>
    /// <param name="columns">Number of trailing columns, from 1 to <see cref="Parser.MaxWordLength"/>.</param>
    /// <returns>The value modulo <c>10^columns</c>, between 0 and <c>10^columns - 1</c>.</returns>
    public abstract Int128 EvaluateTail(ReadOnlySpan<int> digits, int columns);

    /// <summary>Computes <c>10^columns</c>.</summary>
    /// <param name="columns">The exponent.</param>
    /// <returns>The power of ten.</returns>
    protected static Int128 PowerOfTen(int columns)
    {
        Int128 power = 1;
        for (int column = 0; column < columns; column++)
        {
            power *= 10;
        }

        return power;
    }

    /// <summary>Lists the words in order of appearance.</summary>
    /// <returns>Every word, including repetitions.</returns>
    public abstract IEnumerable<string> Words();

    /// <summary>Renders the expression, transforming each word.</summary>
    /// <param name="word">Maps a word to its rendering, for example its digits.</param>
    /// <returns>The rendered expression.</returns>
    public abstract string Render(Func<string, string> word);

    /// <inheritdoc />
    public sealed override string ToString() => Render(static word => word);
}

/// <summary>A word such as <c>MONEY</c>, read as a base 10 number.</summary>
/// <param name="Letters">Upper case letters, most significant first.</param>
public sealed record Word(string Letters) : Expression
{
    /// <inheritdoc />
    public override Int128 Evaluate(ReadOnlySpan<int> digits)
    {
        Int128 value = 0;
        foreach (char letter in Letters)
        {
            value = checked((value * 10) + digits[letter - 'A']);
        }

        return value;
    }

    /// <inheritdoc />
    public override Int128 EvaluateTail(ReadOnlySpan<int> digits, int columns)
    {
        Int128 value = 0;
        for (int index = Math.Max(0, Letters.Length - columns); index < Letters.Length; index++)
        {
            value = (value * 10) + digits[Letters[index] - 'A'];
        }

        return value;
    }

    /// <inheritdoc />
    public override IEnumerable<string> Words() => [Letters];

    /// <inheritdoc />
    public override string Render(Func<string, string> word)
    {
        ArgumentNullException.ThrowIfNull(word);

        return word(Letters);
    }
}

/// <summary>A literal number such as <c>100</c>.</summary>
/// <param name="Value">The value.</param>
public sealed record Number(long Value) : Expression
{
    /// <inheritdoc />
    public override Int128 Evaluate(ReadOnlySpan<int> digits) => Value;

    /// <inheritdoc />
    public override Int128 EvaluateTail(ReadOnlySpan<int> digits, int columns) => Value % PowerOfTen(columns);

    /// <inheritdoc />
    public override IEnumerable<string> Words() => [];

    /// <inheritdoc />
    public override string Render(Func<string, string> word) =>
        Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Addition, subtraction or multiplication of two expressions.</summary>
/// <param name="Operator">One of <c>+</c>, <c>-</c> or <c>*</c>.</param>
/// <param name="Left">Left operand.</param>
/// <param name="Right">Right operand.</param>
public sealed record Binary(char Operator, Expression Left, Expression Right) : Expression
{
    /// <inheritdoc />
    public override Int128 Evaluate(ReadOnlySpan<int> digits)
    {
        Int128 left = Left.Evaluate(digits);
        Int128 right = Right.Evaluate(digits);

        return Operator switch
        {
            '+' => checked(left + right),
            '-' => checked(left - right),
            '*' => checked(left * right),
            _ => throw new InvalidOperationException($"unknown operator '{Operator}'"),
        };
    }

    /// <inheritdoc />
    public override Int128 EvaluateTail(ReadOnlySpan<int> digits, int columns)
    {
        Int128 modulus = PowerOfTen(columns);
        Int128 left = Left.EvaluateTail(digits, columns);
        Int128 right = Right.EvaluateTail(digits, columns);
        Int128 value = Operator switch
        {
            '+' => left + right,
            '-' => left - right + modulus,
            '*' => left * right,
            _ => throw new InvalidOperationException($"unknown operator '{Operator}'"),
        };

        return value % modulus;
    }

    /// <inheritdoc />
    public override IEnumerable<string> Words() => Left.Words().Concat(Right.Words());

    /// <inheritdoc />
    public override string Render(Func<string, string> word)
    {
        int precedence = PrecedenceOf(this);
        string left = Wrap(Left, word, PrecedenceOf(Left) < precedence);
        string right = Wrap(
            Right,
            word,
            PrecedenceOf(Right) < precedence || (PrecedenceOf(Right) == precedence && Operator == '-'));

        return $"{left} {Operator} {right}";
    }

    private static int PrecedenceOf(Expression expression) => expression switch
    {
        Binary { Operator: '*' } => 2,
        Binary => 1,
        _ => 3,
    };

    private static string Wrap(Expression operand, Func<string, string> word, bool parenthesize) =>
        parenthesize ? $"({operand.Render(word)})" : operand.Render(word);
}

/// <summary>An equation between two expressions.</summary>
/// <param name="Left">Left side.</param>
/// <param name="Right">Right side.</param>
public sealed record Equation(Expression Left, Expression Right)
{
    /// <summary>Checks whether both sides are equal for the given digits.</summary>
    /// <param name="digits">Digit assigned to each letter, indexed by <c>letter - 'A'</c>.</param>
    /// <returns>True when the equation holds.</returns>
    public bool Holds(ReadOnlySpan<int> digits) => Left.Evaluate(digits) == Right.Evaluate(digits);

    /// <summary>Checks the equation modulo <c>10^columns</c>, a necessary condition for it to hold.</summary>
    /// <param name="digits">Digit assigned to each letter, indexed by <c>letter - 'A'</c>.</param>
    /// <param name="columns">Number of trailing columns.</param>
    /// <returns>True when the last <paramref name="columns"/> digits of both sides agree.</returns>
    public bool HoldsInLastColumns(ReadOnlySpan<int> digits, int columns) =>
        Left.EvaluateTail(digits, columns) == Right.EvaluateTail(digits, columns);

    /// <summary>Gets the length of the longest word, the number of columns of the equation.</summary>
    public int Columns => Words().Select(static word => word.Length).DefaultIfEmpty(0).Max();

    /// <summary>Lists the words of both sides in order of appearance.</summary>
    /// <returns>Every word, including repetitions.</returns>
    public IEnumerable<string> Words() => Left.Words().Concat(Right.Words());

    /// <summary>Renders the equation, transforming each word.</summary>
    /// <param name="word">Maps a word to its rendering.</param>
    /// <returns>The rendered equation.</returns>
    public string Render(Func<string, string> word) => $"{Left.Render(word)} = {Right.Render(word)}";

    /// <inheritdoc />
    public override string ToString() => Render(static word => word);
}
