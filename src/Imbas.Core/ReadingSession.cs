using Imbas.Core.Epub;

namespace Imbas.Core;

/// <summary>
/// An open book and the reader's place in it. Every change of position is written to the
/// <see cref="ReadingStateStore"/>, so the next launch can pick up where the reader stopped.
/// </summary>
public sealed class ReadingSession : IDisposable
{
    private readonly ReadingStateStore _store;
    private readonly string _bookKey;

    private ReadingSession(EpubBook book, string filePath, ReadingStateStore store)
    {
        Book = book;
        FilePath = Path.GetFullPath(filePath);
        _store = store;
        _bookKey = ReadingStateStore.KeyFor(book, FilePath);

        var saved = store.GetPosition(_bookKey) ?? ReadingPosition.Start;
        Position = saved.ChapterIndex < book.Chapters.Count ? saved : ReadingPosition.Start;
        Save();
    }

    public EpubBook Book { get; }

    public string FilePath { get; }

    public ReadingPosition Position { get; private set; }

    public EpubChapter? CurrentChapter =>
        Position.ChapterIndex < Book.Chapters.Count ? Book.Chapters[Position.ChapterIndex] : null;

    public bool HasPreviousChapter => Position.ChapterIndex > 0;

    public bool HasNextChapter => Position.ChapterIndex < Book.Chapters.Count - 1;

    /// <summary>Opens a book at the position saved for it, or at the start.</summary>
    /// <exception cref="EpubFormatException">The file is not a readable EPUB.</exception>
    public static ReadingSession Open(string filePath, ReadingStateStore store)
    {
        var book = EpubReader.Open(filePath);
        try
        {
            return new ReadingSession(book, filePath, store);
        }
        catch
        {
            book.Dispose();
            throw;
        }
    }

    /// <summary>The current chapter as HTML for the reading pane.</summary>
    public string ReadCurrentChapterHtml() =>
        CurrentChapter is null ? "" : Book.ReadChapterHtml(Position.ChapterIndex);

    /// <summary>Moves to the start of a chapter.</summary>
    public void GoToChapter(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Book.Chapters.Count);

        Position = new ReadingPosition(index, 0);
        Save();
    }

    /// <summary>Records how far through the current chapter the reader has scrolled.</summary>
    public void UpdateProgress(double progress)
    {
        progress = double.IsFinite(progress) ? Math.Clamp(progress, 0, 1) : 0;
        Position = new ReadingPosition(Position.ChapterIndex, progress);
        Save();
    }

    public void Dispose() => Book.Dispose();

    private void Save() => _store.Save(_bookKey, FilePath, Position);
}
