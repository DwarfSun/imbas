namespace Imbas.Core.Tests;

public class LibraryTests
{
    private static readonly Book Dune = Book.Create("Dune", ["Frank Herbert"], "dune.epub");
    private static readonly Book Emma = Book.Create("Emma", ["Jane Austen"], "emma.txt");

    [Fact]
    public void Add_RejectsDuplicateId()
    {
        var library = new Library();

        Assert.True(library.Add(Dune));
        Assert.False(library.Add(Dune with { Title = "Dune (copy)" }));
        Assert.Equal(1, library.Count);
    }

    [Fact]
    public void Find_ReturnsBookOrNull()
    {
        var library = new Library();
        library.Add(Dune);

        Assert.Same(Dune, library.Find(Dune.Id));
        Assert.Null(library.Find(Emma.Id));
    }

    [Theory]
    [InlineData("dune", "Dune")]
    [InlineData("AUSTEN", "Emma")]
    [InlineData("  herb ", "Dune")]
    public void Search_MatchesTitleOrAuthorIgnoringCase(string query, string expectedTitle)
    {
        var library = new Library();
        library.Add(Dune);
        library.Add(Emma);

        var result = Assert.Single(library.Search(query));
        Assert.Equal(expectedTitle, result.Title);
    }

    [Fact]
    public void Search_WithBlankQuery_ReturnsEverything()
    {
        var library = new Library();
        library.Add(Dune);
        library.Add(Emma);

        Assert.Equal(2, library.Search(" ").Count());
    }

    [Fact]
    public void GetPosition_DefaultsToStart()
    {
        var library = new Library();
        library.Add(Dune);

        Assert.Equal(ReadingPosition.Start, library.GetPosition(Dune.Id));
    }

    [Fact]
    public void SetPosition_IsRemembered()
    {
        var library = new Library();
        library.Add(Dune);
        var position = new ReadingPosition(3, 0.25);

        library.SetPosition(Dune.Id, position);

        Assert.Equal(position, library.GetPosition(Dune.Id));
    }

    [Fact]
    public void Positions_ForUnknownBook_Throw()
    {
        var library = new Library();

        Assert.Throws<KeyNotFoundException>(() => library.GetPosition(Dune.Id));
        Assert.Throws<KeyNotFoundException>(() => library.SetPosition(Dune.Id, ReadingPosition.Start));
    }

    [Fact]
    public void Remove_ForgetsPosition()
    {
        var library = new Library();
        library.Add(Dune);
        library.SetPosition(Dune.Id, new ReadingPosition(2, 0.5));

        Assert.True(library.Remove(Dune.Id));
        library.Add(Dune);

        Assert.Equal(ReadingPosition.Start, library.GetPosition(Dune.Id));
    }
}
