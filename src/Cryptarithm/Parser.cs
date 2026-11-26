using System.Globalization;

namespace Cryptarithm;

/// <summary>Raised when an equation cannot be parsed or a puzzle is malformed.</summary>
public sealed class PuzzleFormatException : FormatException
{
    /// <summary>Creates the exception.</summary>
    public PuzzleFormatException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What is wrong.</param>
    public PuzzleFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The cause.</param>
    public PuzzleFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Recursive descent parser for equations such as <c>SEND + MORE = MONEY</c> or
/// <c>LMN * UX + Y = RSTUN</c>.
/// </summary>
/// <remarks>
/// Grammar: <c>equation := sum '=' sum</c>, <c>sum := product (('+' | '-') product)*</c>,
/// <c>product := factor ('*' factor)*</c>, <c>factor := word | number | '(' sum ')'</c>.
/// Words are letters A to Z (lower case is accepted) and numbers are decimal literals.
/// </remarks>
public sealed class Parser
{
    /// <summary>Longest word accepted, so that products of two words fit in 128 bits.</summary>
    public const int MaxWordLength = 18;

    private readonly string text;
    private int position;

    private Parser(string text)
    {
        this.text = text;
    }

    /// <summary>Parses one equation.</summary>
    /// <param name="text">The equation.</param>
    /// <returns>The parsed equation.</returns>
    /// <exception cref="PuzzleFormatException">The text is not a valid equation.</exception>
    public static Equation ParseEquation(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parser = new Parser(text);
        Expression left = parser.ParseSum();
        parser.Expect('=');
        Expression right = parser.ParseSum();
        parser.SkipSpaces();
        if (parser.position < text.Length)
        {
            throw parser.Error($"unexpected '{text[parser.position]}'");
        }

        return new Equation(left, right);
    }

    private Expression ParseSum()
    {
        Expression result = ParseProduct();
        while (Peek() is '+' or '-')
        {
            char op = text[position++];
            result = new Binary(op, result, ParseProduct());
        }

        return result;
    }

    private Expression ParseProduct()
    {
        Expression result = ParseFactor();
        while (Peek() is '*')
        {
            position++;
            result = new Binary('*', result, ParseFactor());
        }

        return result;
    }

    private Expression ParseFactor()
    {
        char? next = Peek();
        if (next == '(')
        {
            position++;
            Expression inner = ParseSum();
            Expect(')');

            return inner;
        }

        if (next is char letter && char.IsAsciiLetter(letter))
        {
            string word = Take(char.IsAsciiLetter).ToUpperInvariant();

            return word.Length <= MaxWordLength
                ? new Word(word)
                : throw Error($"word {word} is longer than {MaxWordLength} letters");
        }

        if (next is char digit && char.IsAsciiDigit(digit))
        {
            string digits = Take(char.IsAsciiDigit);

            return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out long value)
                ? new Number(value)
                : throw Error($"number {digits} is too large");
        }

        throw next is null ? Error("unexpected end of equation") : Error($"unexpected '{next}'");
    }

    private string Take(Func<char, bool> accepts)
    {
        int start = position;
        while (position < text.Length && accepts(text[position]))
        {
            position++;
        }

        return text[start..position];
    }

    private void Expect(char expected)
    {
        if (Peek() != expected)
        {
            throw Error($"expected '{expected}'");
        }

        position++;
    }

    private char? Peek()
    {
        SkipSpaces();

        return position < text.Length ? text[position] : null;
    }

    private void SkipSpaces()
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }
    }

    private PuzzleFormatException Error(string message) =>
        new($"{message} at column {position + 1} of \"{text}\"");
}
