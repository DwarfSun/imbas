using Imbas.Core.Epub;

namespace Imbas.Core.Tests;

public sealed class ReadingStateStoreTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("imbas-tests-");

    private string StatePath => Path.Combine(_dir.FullName, "state", "reading-state.json");

    public void Dispose() => _dir.Delete(recursive: true);

    [Fact]
    public void Constructor_StartsEmptyWithoutFile()
    {
        var store = new ReadingStateStore(StatePath);

        Assert.Null(store.LastOpenedPath);
        Assert.Null(store.GetPosition("id:x"));
    }

    [Fact]
    public void Save_PersistsPositionAndLastBookAcrossInstances()
    {
        var bookPath = Path.Combine(_dir.FullName, "book.epub");
        new ReadingStateStore(StatePath).Save("id:x", bookPath, new ReadingPosition(3, 0.25));

        var reopened = new ReadingStateStore(StatePath);

        Assert.Equal<ReadingPosition?>(new ReadingPosition(3, 0.25), reopened.GetPosition("id:x"));
        Assert.Equal(Path.GetFullPath(bookPath), reopened.LastOpenedPath);
    }

    [Fact]
    public void Save_KeepsPositionsForSeveralBooks()
    {
        var store = new ReadingStateStore(StatePath);
        store.Save("id:a", "a.epub", new ReadingPosition(1, 0.5));
        store.Save("id:b", "b.epub", new ReadingPosition(2, 0.75));
        store.Save("id:a", "a.epub", new ReadingPosition(4, 0.1));

        var reopened = new ReadingStateStore(StatePath);

        Assert.Equal<ReadingPosition?>(new ReadingPosition(4, 0.1), reopened.GetPosition("id:a"));
        Assert.Equal<ReadingPosition?>(new ReadingPosition(2, 0.75), reopened.GetPosition("id:b"));
        Assert.Equal(Path.GetFullPath("a.epub"), reopened.LastOpenedPath);
    }

    [Fact]
    public void Constructor_IgnoresCorruptFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, "{ not json");

        var store = new ReadingStateStore(StatePath);
        Assert.Null(store.LastOpenedPath);

        store.Save("id:x", "x.epub", ReadingPosition.Start);
        Assert.Equal<ReadingPosition?>(ReadingPosition.Start, new ReadingStateStore(StatePath).GetPosition("id:x"));
    }

    [Fact]
    public void GetPosition_IgnoresOutOfRangeValues()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, """{ "positions": { "id:x": { "chapter": 2, "progress": 7 } } }""");

        Assert.Null(new ReadingStateStore(StatePath).GetPosition("id:x"));
    }

    [Fact]
    public void ClearLastOpened_ForgetsBookButKeepsPosition()
    {
        var store = new ReadingStateStore(StatePath);
        store.Save("id:x", "x.epub", new ReadingPosition(1, 0));

        store.ClearLastOpened();

        var reopened = new ReadingStateStore(StatePath);
        Assert.Null(reopened.LastOpenedPath);
        Assert.NotNull(reopened.GetPosition("id:x"));
    }

    [Fact]
    public void KeyFor_UsesIdentifierElsePath()
    {
        using var withId = EpubReader.Open(TestEpub.Epub3().ToStream());
        Assert.Equal("id:urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427", ReadingStateStore.KeyFor(withId, "any.epub"));

        var stream = new TestEpub()
            .With("META-INF/container.xml", TestEpub.Container("content.opf"))
            .With("content.opf", """
                <package xmlns="http://www.idpf.org/2007/opf" version="3.0">
                  <metadata/><manifest/><spine/>
                </package>
                """)
            .ToStream();
        using var withoutId = EpubReader.Open(stream);
        Assert.Equal("path:" + Path.GetFullPath("any.epub"), ReadingStateStore.KeyFor(withoutId, "any.epub"));
    }
}
