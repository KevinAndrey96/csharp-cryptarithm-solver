using System.Globalization;

namespace Cryptarithm.Cli;

/// <summary>Spanish command line interface over the solver.</summary>
public static class CommandLine
{
    /// <summary>At least one solution was found.</summary>
    public const int Found = 0;

    /// <summary>The puzzle has no solution.</summary>
    public const int NotFound = 1;

    /// <summary>Invalid arguments or equations.</summary>
    public const int UsageError = 2;

    /// <summary>Solutions printed in full before only counting the rest.</summary>
    public const int PrintedSolutions = 10;

    private const string RepeatFlag = "--repetir-digitos";

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Runs a command.</summary>
    /// <param name="args">
    /// <c>listar</c>, <c>resolver &lt;nombre&gt;</c> or <c>ecuacion "&lt;ecuación&gt;"...</c>, optionally
    /// followed by <c>--repetir-digitos</c>.
    /// </param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <returns>The exit code.</returns>
    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        bool distinct = !args.Contains(RepeatFlag);
        var rest = args.Where(static arg => arg != RepeatFlag).ToList();
        try
        {
            switch (rest)
            {
                case ["listar"]:
                    List(output);

                    return Found;
                case ["resolver", string name] when Catalog.All.TryGetValue(name, out Puzzle? puzzle):
                    return Solve(puzzle, distinct, output);
                case ["resolver", string name]:
                    return Usage(error, $"no existe el acertijo \"{name}\"");
                case ["ecuacion", .. var equations] when equations.Count > 0:
                    return Solve(Puzzle.Parse("Acertijo personalizado", equations), distinct, output);
                default:
                    return Usage(error, "comando no reconocido");
            }
        }
        catch (PuzzleFormatException e)
        {
            return Usage(error, e.Message);
        }
    }

    private static void List(TextWriter output)
    {
        foreach ((string name, Puzzle puzzle) in Catalog.All)
        {
            output.WriteLine($"{name,-16} {puzzle.Description}");
        }
    }

    private static int Solve(Puzzle puzzle, bool distinct, TextWriter output)
    {
        output.WriteLine(puzzle.Description);
        foreach (Equation equation in puzzle.Equations)
        {
            output.WriteLine($"  {equation}");
        }

        SolveResult result = Solver.Solve(puzzle, new SolverOptions(DistinctDigits: distinct));
        foreach ((Solution solution, int index) in result.Solutions.Take(PrintedSolutions).Select((s, i) => (s, i + 1)))
        {
            output.WriteLine();
            output.WriteLine($"Solución {index}: {string.Join(' ', solution.Digits.Select(static d => $"{d.Key}={d.Value}"))}");
            foreach (Equation equation in puzzle.Equations)
            {
                output.WriteLine($"  {equation.Render(solution.Spell)}");
            }
        }

        int count = result.Solutions.Count;
        output.WriteLine();
        output.WriteLine(string.Format(
            Spanish,
            "{0:N0} {1}, {2:N0} asignaciones probadas{3}.",
            count,
            count == 1 ? "solución" : "soluciones",
            result.Assignments,
            distinct ? string.Empty : " (dígitos repetidos permitidos)"));

        return count > 0 ? Found : NotFound;
    }

    private static int Usage(TextWriter error, string message)
    {
        error.WriteLine($"error: {message}");
        error.WriteLine("uso: cryptarithm-cli listar");
        error.WriteLine("     cryptarithm-cli resolver <nombre> [--repetir-digitos]");
        error.WriteLine("     cryptarithm-cli ecuacion \"SEND + MORE = MONEY\" [...] [--repetir-digitos]");

        return UsageError;
    }
}
