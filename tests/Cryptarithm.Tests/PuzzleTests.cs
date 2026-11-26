namespace Cryptarithm.Tests;

public sealed class PuzzleTests
{
    [Fact]
    public void CollectsLettersInOrderOfAppearance()
    {
        Assert.Equal("SENDMORY", Catalog.SendMoreMoney.Letters);
        Assert.Equal("ULMNRTYXSJ", Catalog.Division.Letters);
    }

    [Fact]
    public void OnlyWordsOfTwoOrMoreLettersHaveLeadingLetters()
    {
        Assert.Equal("SM", Catalog.SendMoreMoney.LeadingLetters);
        Assert.Equal("LRTU", Catalog.Division.LeadingLetters);
    }

    [Fact]
    public void RejectsPuzzlesWithoutEquationsOrLetters()
    {
        Assert.Throws<PuzzleFormatException>(() => Puzzle.Parse("vacío"));
        Assert.Throws<PuzzleFormatException>(() => Puzzle.Parse("sin letras", "1 + 1 = 2"));
    }

    [Fact]
    public void CatalogListsEveryPuzzleByName()
    {
        Assert.Equal(["division", "send-more-money", "tehe-ahaha"], Catalog.All.Keys);
        Assert.All(Catalog.All.Values, static puzzle => Assert.False(string.IsNullOrWhiteSpace(puzzle.Description)));
    }
}
