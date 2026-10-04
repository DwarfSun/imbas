namespace Imbas.Core;

/// <summary>
/// The reader's collection of books and where they are in each one.
/// </summary>
public sealed class Library
{
    private readonly Dictionary<Guid, Book> _books = [];
    private readonly Dictionary<Guid, ReadingPosition> _positions = [];

    public IReadOnlyCollection<Book> Books => _books.Values;

    public int Count => _books.Count;

    /// <summary>
    /// Adds a book. Returns false if a book with the same id is already in the library.
    /// </summary>
    public bool Add(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);
        return _books.TryAdd(book.Id, book);
    }

    /// <summary>
    /// Removes a book and forgets its reading position.
    /// </summary>
    public bool Remove(Guid bookId)
    {
        _positions.Remove(bookId);
        return _books.Remove(bookId);
    }

    public Book? Find(Guid bookId) => _books.GetValueOrDefault(bookId);

    /// <summary>
    /// Finds books whose title or any author contains the query, ignoring case.
    /// </summary>
    public IEnumerable<Book> Search(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = query.Trim();
        if (q.Length == 0)
            return Books;

        return Books.Where(b =>
            b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || b.Authors.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Returns where the reader left off, or the start of the book if they never opened it.
    /// </summary>
    public ReadingPosition GetPosition(Guid bookId)
    {
        EnsureContains(bookId);
        return _positions.GetValueOrDefault(bookId, ReadingPosition.Start);
    }

    public void SetPosition(Guid bookId, ReadingPosition position)
    {
        EnsureContains(bookId);
        _positions[bookId] = position;
    }

    private void EnsureContains(Guid bookId)
    {
        if (!_books.ContainsKey(bookId))
            throw new KeyNotFoundException($"No book with id {bookId} is in the library.");
    }
}
