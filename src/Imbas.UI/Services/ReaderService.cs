using Imbas.Core;
using Imbas.Core.Epub;

namespace Imbas.UI.Services;

/// <summary>Opens and closes books for the UI and keeps the library list in step.</summary>
public sealed class ReaderService : IDisposable
{
    private readonly Library _library;
    private readonly ReadingStateStore _store;
    private readonly string _booksDirectory;
    private bool _restoreAttempted;

    public ReaderService(Library library, ReadingStateStore store, string booksDirectory)
    {
        _library = library;
        _store = store;
        _booksDirectory = booksDirectory;
    }

    /// <summary>The open book, or null while the library is showing.</summary>
    public ReadingSession? Session { get; private set; }

    /// <summary>Why the last book failed to open, if it did.</summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Reopens the book that was open when the app last closed. Only the first call in a run
    /// does anything, so returning to the library doesn't bounce the reader back into the book.
    /// </summary>
    public bool TryRestoreLastBook()
    {
        if (_restoreAttempted)
            return false;

        _restoreAttempted = true;
        if (_store.LastOpenedPath is not { } path)
            return false;

        if (File.Exists(path) && Open(path, import: false))
            return true;

        _store.ClearLastOpened();
        Error = null;
        return false;
    }

    /// <summary>Opens a book, first copying it into the app's books folder when <paramref name="import"/> is set.</summary>
    public bool Open(string path, bool import = true)
    {
        _restoreAttempted = true;
        ReadingSession session;
        try
        {
            var bookPath = import ? BookFiles.Import(path, _booksDirectory) : path;
            session = ReadingSession.Open(bookPath, _store);
        }
        catch (Exception ex) when (ex is EpubFormatException or IOException or UnauthorizedAccessException)
        {
            Error = $"Couldn't open {Path.GetFileName(path)}: {ex.Message}";
            return false;
        }

        Session?.Dispose();
        Session = session;
        Error = null;
        AddToLibrary(session);
        return true;
    }

    /// <summary>Closes the book. The next launch starts at the library.</summary>
    public void Close()
    {
        Session?.Dispose();
        Session = null;
        _store.ClearLastOpened();
    }

    public void Dispose() => Session?.Dispose();

    private void AddToLibrary(ReadingSession session)
    {
        if (_library.Books.Any(b => b.FilePath == session.FilePath))
            return;

        var book = session.Book;
        var title = string.IsNullOrWhiteSpace(book.Title) ? Path.GetFileNameWithoutExtension(session.FilePath) : book.Title;
        _library.Add(Book.Create(title, book.Authors, session.FilePath));
    }
}
