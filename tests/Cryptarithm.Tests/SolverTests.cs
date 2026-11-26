namespace Cryptarithm.Tests;

public sealed class SolverTests
{
    [Fact]
    public void SolvesTheClassDivision()
    {
        SolveResult result = Solver.Solve(Catalog.Division);

        Solution solution = Assert.Single(result.Solutions);
        Assert.Equal("603", solution.Spell("LMN"));
        Assert.Equal("97", solution.Spell("UX"));
        Assert.Equal("2", solution.Spell("Y"));
        Assert.Equal("58493", solution.Spell("RSTUN"));
    }

    [Fact]
    public void TheDivisionIsUniqueEvenWithRepeatedDigits()
    {
        SolveResult result = Solver.Solve(Catalog.Division, new SolverOptions(DistinctDigits: false));

        Assert.Equal("58493", Assert.Single(result.Solutions).Spell("RSTUN"));
    }

    [Fact]
    public void SolvesTheCommentedOutAddition()
    {
        Solution solution = Assert.Single(Solver.Solve(Catalog.TeheAhaha).Solutions);

        Assert.Equal("5272 + 47474 = 52746", Catalog.TeheAhaha.Equations[0].Render(solution.Spell));
    }

    [Fact]
    public void SolvesSendMoreMoneyWithFewAssignments()
    {
        SolveResult result = Solver.Solve(Catalog.SendMoreMoney);

        Assert.Equal("10652", Assert.Single(result.Solutions).Spell("MONEY"));
        Assert.True(result.Assignments < 10_000, $"{result.Assignments} assignments");
    }

    [Fact]
    public void RepeatedDigitsWidenTheSearch()
    {
        SolveResult result = Solver.Solve(Catalog.SendMoreMoney, new SolverOptions(DistinctDigits: false));

        Assert.Equal(155, result.Solutions.Count);
    }

    [Fact]
    public void StopsAtTheRequestedNumberOfSolutions()
    {
        Puzzle puzzle = Puzzle.Parse("varias", "A + B = C");

        Assert.Equal(3, Solver.Solve(puzzle, new SolverOptions(MaxSolutions: 3)).Solutions.Count);
    }

    [Fact]
    public void AssignsLettersFromTheUnitsColumnUp()
    {
        Assert.Equal("DEYNROSM", Solver.AssignmentOrder(Catalog.SendMoreMoney));
    }

    [Fact]
    public void LeadingLettersAreNeverZero()
    {
        Puzzle puzzle = Puzzle.Parse("ceros", "AB = 5");

        Assert.Empty(Solver.Solve(puzzle).Solutions);
    }

    [Fact]
    public void ElevenLettersCannotTakeDistinctDigits()
    {
        Puzzle puzzle = Puzzle.Parse("once", "ABCDEFGHIJK = ABCDEFGHIJK");

        Assert.Throws<PuzzleFormatException>(() => Solver.Solve(puzzle));
        Solution first = Solver.Solve(puzzle, new SolverOptions(DistinctDigits: false, MaxSolutions: 1)).Solutions[0];

        Assert.Equal("ABCDEFGHIJK", string.Concat(first.Digits.Keys));
    }

    [Fact]
    public void EquationsWithoutLettersAreStillChecked()
    {
        Assert.Empty(Solver.Solve(Puzzle.Parse("falsa", "A = A", "1 = 2")).Solutions);
        Assert.Equal(10, Solver.Solve(Puzzle.Parse("cierta", "A = A", "2 = 2")).Solutions.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AgreesWithExhaustiveEnumerationOnRandomPuzzles(bool distinct)
    {
        var random = new Random(distinct ? 1924 : 2019);
        string[] templates = ["AB + CB = DE", "AB * C = DEA", "ABC - DE = CA", "A * B + C = DE", "AB + BA = CDC"];
        for (int round = 0; round < 30; round++)
        {
            string letters = string.Concat("ABCDEFGHIJ".OrderBy(_ => random.Next()).Take(5));
            string template = templates[round % templates.Length];
            string equation = string.Concat(template.Select(c => char.IsAsciiLetterUpper(c) ? letters[c - 'A'] : c));
            Puzzle puzzle = Puzzle.Parse("aleatorio", equation);

            var expected = Exhaustive(puzzle, distinct);
            var actual = Solver.Solve(puzzle, new SolverOptions(distinct)).Solutions
                .Select(static s => string.Concat(s.Digits.Values))
                .Order(StringComparer.Ordinal);

            Assert.Equal(expected, actual);
        }
    }

    private static List<string> Exhaustive(Puzzle puzzle, bool distinct)
    {
        string letters = string.Concat(puzzle.Letters.Order());
        var solutions = new List<string>();
        int total = (int)Math.Pow(10, letters.Length);
        var digits = new int[26];
        for (int code = 0; code < total; code++)
        {
            int rest = code;
            for (int index = letters.Length - 1; index >= 0; index--)
            {
                digits[letters[index] - 'A'] = rest % 10;
                rest /= 10;
            }

            var assigned = letters.Select(letter => digits[letter - 'A']).ToList();
            bool valid = (!distinct || assigned.Distinct().Count() == assigned.Count)
                && puzzle.LeadingLetters.All(letter => digits[letter - 'A'] != 0)
                && puzzle.Equations.All(equation => equation.Holds(digits));
            if (valid)
            {
                solutions.Add(string.Concat(assigned));
            }
        }

        return solutions;
    }
}
