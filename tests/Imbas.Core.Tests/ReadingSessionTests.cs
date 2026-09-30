using Imbas.Core.Epub;

namespace Imbas.Core.Tests;

public sealed class ReadingSessionTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("imbas-tests-");

    private string StatePath => Path.Combine(_dir.FullName, "reading-state.json");

    public void Dispose() => _dir.Delete(recursive: true);

    [Fact]
    public void Open_StartsAtTheBeginningOfANewBook()
    {
        var path = TestEpub.Epub3().WriteTo(_dir.FullName);

        using var session = ReadingSession.Open(path, new ReadingStateStore(StatePath));

        Assert.Equal(ReadingPosition.Start, session.Position);
        Assert.Equal("cover", session.CurrentChapter?.Id);
        Assert.False(session.HasPreviousChapter);
        Assert.True(session.HasNextChapter);
        Assert.Equal(Path.GetFullPath(path), new ReadingStateStore(StatePath).LastOpenedPath);
    }

    [Fact]
    public void Open_ResumesWhereTheReaderStopped()
    {
        var path = TestEpub.Epub3().WriteTo(_dir.FullName);
        using (var first = ReadingSession.Open(path, new ReadingStateStore(StatePath)))
        {
            first.GoToChapter(2);
            first.UpdateProgress(0.4);
        }

        using var second = ReadingSession.Open(path, new ReadingStateStore(StatePath));

        Assert.Equal(new ReadingPosition(2, 0.4), second.Position);
        Assert.False(second.HasNextChapter);
        Assert.Contains("The end.", second.ReadCurrentChapterHtml());
    }

    [Fact]
    public void Open_StartsOverWhenTheSavedChapterNoLongerExists()
    {
        var path = TestEpub.Epub3().WriteTo(_dir.FullName);
        var store = new ReadingStateStore(StatePath);
        store.Save("id:urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427", path, new ReadingPosition(40, 0.5));

        using var session = ReadingSession.Open(path, store);

        Assert.Equal(ReadingPosition.Start, session.Position);
    }

    [Fact]
    public void GoToChapter_ResetsProgress()
    {
        var path = TestEpub.Epub3().WriteTo(_dir.FullName);
        using var session = ReadingSession.Open(path, new ReadingStateStore(StatePath));
        session.UpdateProgress(0.9);

        session.GoToChapter(1);

        Assert.Equal(new ReadingPosition(1, 0), session.Position);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.GoToChapter(3));
    }

    [Theory]
    [InlineData(-0.5, 0)]
    [InlineData(1.5, 1)]
    [InlineData(double.NaN, 0)]
    public void UpdateProgress_ClampsToAChapter(double progress, double expected)
    {
        var path = TestEpub.Epub3().WriteTo(_dir.FullName);
        using var session = ReadingSession.Open(path, new ReadingStateStore(StatePath));

        session.UpdateProgress(progress);

        Assert.Equal(expected, session.Position.ChapterProgress);
    }

    [Fact]
    public void Open_RejectsFilesThatAreNotEpubs()
    {
        var path = Path.Combine(_dir.FullName, "notes.epub");
        File.WriteAllText(path, "not a book");

        Assert.Throws<EpubFormatException>(() => ReadingSession.Open(path, new ReadingStateStore(StatePath)));
    }
}
