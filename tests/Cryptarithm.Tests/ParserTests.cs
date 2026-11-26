namespace Cryptarithm.Tests;

public sealed class ParserTests
{
    private static int[] Digits(params (char Letter, int Digit)[] pairs)
    {
        var digits = new int[26];
        foreach ((char letter, int digit) in pairs)
        {
            digits[letter - 'A'] = digit;
        }

        return digits;
    }

    [Theory]
    [InlineData("SEND + MORE = MONEY", "SEND + MORE = MONEY")]
    [InlineData("send+more=money", "SEND + MORE = MONEY")]
    [InlineData("A - (B + C) = D", "A - (B + C) = D")]
    [InlineData("(A + B) * C = D", "(A + B) * C = D")]
    [InlineData("A + (B * C) = D", "A + B * C = D")]
    [InlineData("A - (B - C) = D", "A - (B - C) = D")]
    [InlineData("((A)) * 10 = AB - B", "A * 10 = AB - B")]
    public void RendersWithTheMinimumParentheses(string text, string expected)
    {
        Assert.Equal(expected, Parser.ParseEquation(text).ToString());
    }

    [Fact]
    public void MultiplicationBindsTighterThanAddition()
    {
        Equation equation = Parser.ParseEquation("A + B * C = D");
        int[] digits = Digits(('A', 2), ('B', 3), ('C', 4), ('D', 14));

        Assert.True(equation.Holds(digits));
    }

    [Fact]
    public void SubtractionIsLeftAssociative()
    {
        Equation equation = Parser.ParseEquation("A - B - C = D");
        int[] digits = Digits(('A', 9), ('B', 3), ('C', 2), ('D', 4));

        Assert.True(equation.Holds(digits));
    }

    [Theory]
    [InlineData("", "unexpected end of equation at column 1")]
    [InlineData("SEND + MORE", "expected '=' at column 12")]
    [InlineData("SEND + = X", "unexpected '=' at column 8")]
    [InlineData("A = B = C", "unexpected '=' at column 7")]
    [InlineData("(A + B = C", "expected ')' at column 8")]
    [InlineData("A / B = C", "expected '=' at column 3")]
    [InlineData("ABCDEFGHIJKLMNOPQRS = A", "longer than 18 letters")]
    [InlineData("A = 99999999999999999999", "number 99999999999999999999 is too large")]
    public void ReportsWhereTheEquationBreaks(string text, string fragment)
    {
        var error = Assert.Throws<PuzzleFormatException>(() => Parser.ParseEquation(text));

        Assert.Contains(fragment, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TailEvaluationAgreesWithFullEvaluationModuloPowersOfTen()
    {
        Equation equation = Parser.ParseEquation("LMN * UX + Y - 7 = RSTUN * (A - B)");
        var random = new Random(2019);
        for (int round = 0; round < 500; round++)
        {
            int[] digits = [.. Enumerable.Range(0, 26).Select(_ => random.Next(10))];
            for (int columns = 1; columns <= 6; columns++)
            {
                Int128 modulus = Int128.Parse("1" + new string('0', columns), System.Globalization.CultureInfo.InvariantCulture);
                Int128 left = ((equation.Left.Evaluate(digits) % modulus) + modulus) % modulus;
                Int128 right = ((equation.Right.Evaluate(digits) % modulus) + modulus) % modulus;

                Assert.Equal(left, equation.Left.EvaluateTail(digits, columns));
                Assert.Equal(right, equation.Right.EvaluateTail(digits, columns));
            }
        }
    }

    [Fact]
    public void ExpressionsListTheirWords()
    {
        Equation equation = Parser.ParseEquation("U * LMN + 3 = RTYX");

        Assert.Equal(["U", "LMN", "RTYX"], equation.Words());
        Assert.Equal(4, equation.Columns);
    }

    [Fact]
    public void ExpressionsRenderOnTheirOwn()
    {
        Assert.Equal("(A + 1) * BC", Parser.ParseEquation("(A + 1) * BC = D").Left.ToString());
    }

    [Fact]
    public void UnknownOperatorsAreRejectedWhenEvaluated()
    {
        var division = new Binary('/', new Word("A"), new Number(2));
        int[] digits = new int[26];

        Assert.Throws<InvalidOperationException>(() => division.Evaluate(digits));
        Assert.Throws<InvalidOperationException>(() => division.EvaluateTail(digits, 1));
    }

    [Fact]
    public void PuzzleFormatExceptionKeepsItsCause()
    {
        var cause = new InvalidOperationException("cause");

        Assert.Same(cause, new PuzzleFormatException("message", cause).InnerException);
        Assert.NotNull(new PuzzleFormatException().Message);
    }
}
