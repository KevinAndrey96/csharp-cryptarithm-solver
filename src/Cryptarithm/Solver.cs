using System.Collections.ObjectModel;

namespace Cryptarithm;

/// <summary>A digit for every letter of a puzzle.</summary>
/// <param name="Digits">Digit assigned to each letter, sorted by letter.</param>
public sealed record Solution(IReadOnlyDictionary<char, int> Digits)
{
    /// <summary>Writes a word with its digits.</summary>
    /// <param name="word">Upper case word made of the puzzle letters.</param>
    /// <returns>The digits, for example <c>9567</c> for <c>SEND</c>.</returns>
    public string Spell(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        return string.Concat(word.Select(letter => (char)('0' + Digits[letter])));
    }
}

/// <summary>Outcome of a search.</summary>
/// <param name="Solutions">Solutions found, in the order the search reached them.</param>
/// <param name="Assignments">Letter assignments tried, a measure of the work done.</param>
public sealed record SolveResult(IReadOnlyList<Solution> Solutions, long Assignments);

/// <summary>Search options.</summary>
/// <param name="DistinctDigits">Whether different letters must take different digits.</param>
/// <param name="MaxSolutions">Stop after this many solutions.</param>
public sealed record SolverOptions(bool DistinctDigits = true, int MaxSolutions = int.MaxValue);

/// <summary>
/// Backtracking search that assigns letters column by column, from the units up, the way the puzzles
/// are solved by hand.
/// </summary>
/// <remarks>
/// Addition, subtraction and multiplication commute with taking the result modulo <c>10^k</c>, so
/// once the last <c>k</c> letters of every word of an equation have a digit, the last <c>k</c> digits
/// of both sides must already agree. Each equation is checked again after every completed column,
/// which discards wrong partial assignments long before the leftmost letters are reached.
/// </remarks>
public static class Solver
{
    private const int Alphabet = 26;

    /// <summary>Finds the assignments that satisfy every equation of the puzzle.</summary>
    /// <param name="puzzle">The puzzle.</param>
    /// <param name="options">Search options, distinct digits by default.</param>
    /// <returns>The solutions and the work done.</returns>
    /// <exception cref="PuzzleFormatException">Distinct digits were requested for more than 10 letters.</exception>
    public static SolveResult Solve(Puzzle puzzle, SolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        options ??= new SolverOptions();
        if (options.DistinctDigits && puzzle.Letters.Length > Puzzle.MaxDistinctLetters)
        {
            throw new PuzzleFormatException(
                $"{puzzle.Letters.Length} letters cannot take {Puzzle.MaxDistinctLetters} distinct digits");
        }

        var search = new Search(puzzle, options);
        search.Run(0);

        return new SolveResult(search.Solutions.AsReadOnly(), search.Assignments);
    }

    /// <summary>Orders the letters by column, units first, and by order of appearance within a column.</summary>
    /// <param name="puzzle">The puzzle.</param>
    /// <returns>The letters in assignment order.</returns>
    public static string AssignmentOrder(Puzzle puzzle)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        var words = puzzle.Equations.SelectMany(static equation => equation.Words()).ToList();
        int columns = puzzle.Equations.Max(static equation => equation.Columns);

        return string.Concat(Enumerable.Range(1, columns)
            .SelectMany(column => words.Where(word => word.Length >= column).Select(word => word[^column]))
            .Distinct());
    }

    private readonly record struct Check(Equation Equation, int Columns, bool Exact);

    private sealed class Search
    {
        private readonly string order;
        private readonly bool[] leading = new bool[Alphabet];
        private readonly Check[][] checks;
        private readonly int[] digits = new int[Alphabet];
        private readonly bool[] used = new bool[10];
        private readonly SolverOptions options;

        public Search(Puzzle puzzle, SolverOptions options)
        {
            this.options = options;
            order = AssignmentOrder(puzzle);
            foreach (char letter in puzzle.LeadingLetters)
            {
                leading[letter - 'A'] = true;
            }

            var byDepth = Enumerable.Range(0, order.Length).Select(static _ => new List<Check>()).ToArray();
            foreach (Equation equation in puzzle.Equations)
            {
                var words = equation.Words().ToList();
                for (int columns = 1; columns < equation.Columns; columns++)
                {
                    byDepth[DepthOf(words.SelectMany(word => word[Math.Max(0, word.Length - columns)..]))]
                        .Add(new Check(equation, columns, Exact: false));
                }

                byDepth[DepthOf(words.SelectMany(static word => word))].Add(new Check(equation, 0, Exact: true));
            }

            checks = [.. byDepth.Select(static list => list.ToArray())];
        }

        public List<Solution> Solutions { get; } = [];

        public long Assignments { get; private set; }

        public void Run(int depth)
        {
            if (depth == order.Length)
            {
                Solutions.Add(new Solution(Snapshot()));

                return;
            }

            int letter = order[depth] - 'A';
            for (int digit = leading[letter] ? 1 : 0; digit < 10 && Solutions.Count < options.MaxSolutions; digit++)
            {
                if (options.DistinctDigits && used[digit])
                {
                    continue;
                }

                Assignments++;
                digits[letter] = digit;
                used[digit] = true;
                if (Passes(checks[depth]))
                {
                    Run(depth + 1);
                }

                used[digit] = false;
            }
        }

        private bool Passes(Check[] pending)
        {
            foreach (Check check in pending)
            {
                bool holds = check.Exact
                    ? check.Equation.Holds(digits)
                    : check.Equation.HoldsInLastColumns(digits, check.Columns);
                if (!holds)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Depth at which the last of the letters gets a digit; 0 when there are none.</summary>
        private int DepthOf(IEnumerable<char> letters) =>
            letters.Select(letter => order.IndexOf(letter, StringComparison.Ordinal)).DefaultIfEmpty(0).Max();

        private ReadOnlyDictionary<char, int> Snapshot() =>
            order.OrderBy(static letter => letter)
                .ToDictionary(static letter => letter, letter => digits[letter - 'A'])
                .AsReadOnly();
    }
}
