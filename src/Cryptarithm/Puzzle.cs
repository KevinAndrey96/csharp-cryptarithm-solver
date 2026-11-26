namespace Cryptarithm;

/// <summary>A set of equations that must hold at the same time.</summary>
public sealed class Puzzle
{
    /// <summary>Different letters available when each one needs its own digit.</summary>
    public const int MaxDistinctLetters = 10;

    private Puzzle(string description, IReadOnlyList<Equation> equations)
    {
        Description = description;
        Equations = equations;
        var words = equations.SelectMany(static equation => equation.Words()).ToList();
        Letters = string.Concat(words.SelectMany(static word => word).Distinct());
        LeadingLetters = string.Concat(
            words.Where(static word => word.Length > 1).Select(static word => word[0]).Distinct());
    }

    /// <summary>Gets the human readable description.</summary>
    public string Description { get; }

    /// <summary>Gets the equations.</summary>
    public IReadOnlyList<Equation> Equations { get; }

    /// <summary>Gets every letter, in order of first appearance.</summary>
    public string Letters { get; }

    /// <summary>Gets the letters that start a word of two or more letters and so cannot be zero.</summary>
    public string LeadingLetters { get; }

    /// <summary>Parses a puzzle from its equations.</summary>
    /// <param name="description">Human readable description.</param>
    /// <param name="equations">One or more equations.</param>
    /// <returns>The puzzle.</returns>
    /// <exception cref="PuzzleFormatException">An equation is invalid or there are no letters.</exception>
    public static Puzzle Parse(string description, params IEnumerable<string> equations)
    {
        ArgumentNullException.ThrowIfNull(equations);
        var parsed = equations.Select(Parser.ParseEquation).ToList();
        if (parsed.Count == 0)
        {
            throw new PuzzleFormatException("a puzzle needs at least one equation");
        }

        var puzzle = new Puzzle(description, parsed);

        return puzzle.Letters.Length > 0
            ? puzzle
            : throw new PuzzleFormatException("a puzzle needs at least one letter");
    }
}
