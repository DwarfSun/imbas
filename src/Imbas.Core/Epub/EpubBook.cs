using System.IO.Compression;
using System.Text;

namespace Imbas.Core.Epub;

/// <summary>
/// An opened EPUB. Metadata, reading order and table of contents are read up front; chapter
/// content is read from the archive on demand, so the book must be disposed when no longer needed.
/// </summary>
public sealed class EpubBook : IDisposable
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, int> _chapterIndexByPath = new(StringComparer.Ordinal);

    internal EpubBook(
        ZipArchive archive,
        string? identifier,
        string title,
        IReadOnlyList<string> authors,
        string? language,
        IReadOnlyList<EpubChapter> chapters,
        IReadOnlyList<TocEntry> tableOfContents)
    {
        _archive = archive;
        Identifier = identifier;
        Title = title;
        Authors = authors;
        Language = language;
        Chapters = chapters;
        TableOfContents = tableOfContents;
        foreach (var chapter in chapters)
        {
            _chapterIndexByPath.TryAdd(chapter.Path, chapter.Index);
        }
    }

    /// <summary>The package's unique identifier (often an ISBN or UUID), if it declares one.</summary>
    public string? Identifier { get; }

    public string Title { get; }

    public IReadOnlyList<string> Authors { get; }

    public string? Language { get; }

    /// <summary>The documents in reading order.</summary>
    public IReadOnlyList<EpubChapter> Chapters { get; }

    /// <summary>The book's navigation tree. Empty when the book has none.</summary>
    public IReadOnlyList<TocEntry> TableOfContents { get; }

    /// <summary>Reads a chapter's raw XHTML.</summary>
    public string ReadChapterXhtml(int index) => ReadText(GetChapter(index).Path);

    /// <summary>
    /// Reads a chapter as an HTML fragment for a web view, with images inlined and scripts removed.
    /// See <see cref="ChapterHtml"/>.
    /// </summary>
    public string ReadChapterHtml(int index)
    {
        var chapter = GetChapter(index);
        try
        {
            return ChapterHtml.Render(ReadText(chapter.Path), chapter.Path, _chapterIndexByPath, TryReadResource);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new EpubFormatException($"Chapter '{chapter.Path}' is not well-formed XHTML.", ex);
        }
    }

    /// <summary>Reads any file in the archive, such as an image, by its archive path.</summary>
    public byte[] ReadResource(string path)
    {
        using var stream = OpenEntry(path);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private byte[]? TryReadResource(string path) => _archive.GetEntry(path) is null ? null : ReadResource(path);

    public void Dispose() => _archive.Dispose();

    internal string ReadText(string path)
    {
        using var reader = new StreamReader(OpenEntry(path), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private EpubChapter GetChapter(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Chapters.Count);
        return Chapters[index];
    }

    private Stream OpenEntry(string path)
    {
        var entry = _archive.GetEntry(path) ?? throw new EpubFormatException($"The book is missing '{path}'.");
        return entry.Open();
    }
}
