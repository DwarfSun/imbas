namespace Imbas.Core.Epub;

/// <summary>A document in the book's reading order (an EPUB spine item).</summary>
/// <param name="Index">Zero-based position in the reading order.</param>
/// <param name="Id">The manifest id of the document.</param>
/// <param name="Path">The document's path inside the EPUB archive.</param>
/// <param name="Title">The title from the table of contents, if the document is listed there.</param>
/// <param name="IsLinear">False when the spine marks the document as auxiliary (linear="no").</param>
public sealed record EpubChapter(int Index, string Id, string Path, string? Title, bool IsLinear)
{
    /// <summary>The TOC title, or a numbered fallback when the document has none.</summary>
    public string DisplayTitle => Title ?? $"Section {Index + 1}";
}
