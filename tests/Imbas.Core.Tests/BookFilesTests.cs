namespace Imbas.Core.Tests;

public sealed class BookFilesTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("imbas-tests-");

    public void Dispose() => _dir.Delete(recursive: true);

    [Fact]
    public void Import_CopiesTheBookIntoTheFolder()
    {
        var source = TestEpub.Epub3().WriteTo(_dir.FullName, "picked.epub");
        var books = Path.Combine(_dir.FullName, "Books");

        var imported = BookFiles.Import(source, books);

        Assert.Equal(Path.Combine(books, "picked.epub"), imported);
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(imported));
    }

    [Fact]
    public void Import_ReplacesAnOlderCopy()
    {
        var books = Path.Combine(_dir.FullName, "Books");
        Directory.CreateDirectory(books);
        File.WriteAllText(Path.Combine(books, "picked.epub"), "old");
        var source = TestEpub.Epub3().WriteTo(_dir.FullName, "picked.epub");

        var imported = BookFiles.Import(source, books);

        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(imported));
    }

    [Fact]
    public void Import_LeavesABookAlreadyInTheFolder()
    {
        var books = Directory.CreateDirectory(Path.Combine(_dir.FullName, "Books")).FullName;
        var path = TestEpub.Epub3().WriteTo(books);

        Assert.Equal(path, BookFiles.Import(path, books));
        Assert.True(File.Exists(path));
    }
}
