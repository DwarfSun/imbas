namespace Imbas.Core;

/// <summary>
/// A book in the library: its identity, descriptive metadata and where its file lives.
/// </summary>
public sealed record Book
{
    public Book(Guid id, string title, IReadOnlyList<string> authors, string filePath, BookFormat format)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A book needs a non-empty id.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(authors);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Id = id;
        Title = title;
        Authors = authors;
        FilePath = filePath;
        Format = format;
    }

    public Guid Id { get; }

    public string Title { get; init; }

    public IReadOnlyList<string> Authors { get; init; }

    public string FilePath { get; init; }

    public BookFormat Format { get; init; }

    /// <summary>
    /// Creates a book with a fresh id, inferring the format from the file extension.
    /// </summary>
    public static Book Create(string title, IReadOnlyList<string> authors, string filePath) =>
        new(Guid.NewGuid(), title, authors, filePath, BookFormatExtensions.FromPath(filePath));
}
