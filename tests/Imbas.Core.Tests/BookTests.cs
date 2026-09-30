namespace Imbas.Core.Tests;

public class BookTests
{
    [Theory]
    [InlineData("books/dune.epub", BookFormat.Epub)]
    [InlineData("books/Manual.PDF", BookFormat.Pdf)]
    [InlineData("notes.txt", BookFormat.PlainText)]
    [InlineData("comic.cbz", BookFormat.Unknown)]
    [InlineData("no-extension", BookFormat.Unknown)]
    public void FromPath_InfersFormatFromExtension(string path, BookFormat expected)
    {
        Assert.Equal(expected, BookFormatExtensions.FromPath(path));
    }

    [Fact]
    public void Create_AssignsIdAndFormat()
    {
        var book = Book.Create("Dune", ["Frank Herbert"], "dune.epub");

        Assert.NotEqual(Guid.Empty, book.Id);
        Assert.Equal(BookFormat.Epub, book.Format);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankTitle(string title)
    {
        Assert.ThrowsAny<ArgumentException>(() => Book.Create(title, [], "a.epub"));
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new Book(Guid.Empty, "Dune", [], "dune.epub", BookFormat.Epub));
    }
}
