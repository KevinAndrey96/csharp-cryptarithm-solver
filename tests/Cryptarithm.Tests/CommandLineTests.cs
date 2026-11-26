using Cryptarithm.Cli;

namespace Cryptarithm.Tests;

public sealed class CommandLineTests : IDisposable
{
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();

    public void Dispose()
    {
        output.Dispose();
        error.Dispose();
    }

    private int Run(params string[] args) => CommandLine.Run(args, output, error);

    [Fact]
    public void ListsTheBundledPuzzles()
    {
        Assert.Equal(CommandLine.Found, Run("listar"));
        Assert.Contains("division", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("send-more-money", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SolvesABundledPuzzleAndShowsEachStep()
    {
        Assert.Equal(CommandLine.Found, Run("resolver", "division"));
        string text = output.ToString();

        Assert.Contains("Solución 1: J=1 L=6 M=0 N=3 R=5 S=8 T=4 U=9 X=7 Y=2", text, StringComparison.Ordinal);
        Assert.Contains("603 * 97 + 2 = 58493", text, StringComparison.Ordinal);
        Assert.Contains("1 solución,", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SolvesCustomEquations()
    {
        Assert.Equal(CommandLine.Found, Run("ecuacion", "TO + GO = OUT"));

        Assert.Contains("21 + 81 = 102", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsPuzzlesWithoutSolution()
    {
        Assert.Equal(CommandLine.NotFound, Run("ecuacion", "A + A = B", "B = 1"));

        Assert.Contains("0 soluciones", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void PrintsOnlyTheFirstSolutionsButCountsAll()
    {
        Assert.Equal(CommandLine.Found, Run("resolver", "send-more-money", "--repetir-digitos"));
        string text = output.ToString();

        Assert.Contains($"Solución {CommandLine.PrintedSolutions}:", text, StringComparison.Ordinal);
        Assert.DoesNotContain($"Solución {CommandLine.PrintedSolutions + 1}:", text, StringComparison.Ordinal);
        Assert.Contains("155 soluciones", text, StringComparison.Ordinal);
        Assert.Contains("dígitos repetidos permitidos", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("comando no reconocido")]
    [InlineData("comando no reconocido", "bailar")]
    [InlineData("comando no reconocido", "ecuacion")]
    [InlineData("no existe el acertijo \"fermat\"", "resolver", "fermat")]
    [InlineData("expected '='", "ecuacion", "SEND + MORE")]
    public void RejectsInvalidInvocations(string message, params string[] args)
    {
        Assert.Equal(CommandLine.UsageError, Run(args));

        Assert.StartsWith($"error: {message}", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("uso: cryptarithm-cli", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }
}
