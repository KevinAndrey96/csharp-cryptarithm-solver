namespace Cryptarithm;

/// <summary>Puzzles that ship with the solver.</summary>
public static class Catalog
{
    /// <summary>
    /// Long division RSTUN / LMN = UX with remainder Y, the class problem of 2019. Each equation is
    /// one step of the written algorithm.
    /// </summary>
    public static Puzzle Division { get; } = Puzzle.Parse(
        "División larga RSTUN ÷ LMN = UX con residuo Y (problema de clase)",
        "U * LMN = RTYX",
        "RSTU - RTYX = TYY",
        "X * LMN = TYYJ",
        "TYYN - TYYJ = Y",
        "LMN * UX + Y = RSTUN");

    /// <summary>Gets the addition left commented out in the 2019 program.</summary>
    public static Puzzle TeheAhaha { get; } = Puzzle.Parse(
        "Suma TEHE + AHAHA = TEHAW (segundo ejercicio de clase)",
        "TEHE + AHAHA = TEHAW");

    /// <summary>Gets the classic puzzle by Henry Dudeney, 1924.</summary>
    public static Puzzle SendMoreMoney { get; } = Puzzle.Parse(
        "SEND + MORE = MONEY (Henry Dudeney, 1924)",
        "SEND + MORE = MONEY");

    /// <summary>Gets every bundled puzzle by its command line name.</summary>
    public static IReadOnlyDictionary<string, Puzzle> All { get; } = new SortedDictionary<string, Puzzle>(StringComparer.Ordinal)
    {
        ["division"] = Division,
        ["send-more-money"] = SendMoreMoney,
        ["tehe-ahaha"] = TeheAhaha,
    }.AsReadOnly();
}
