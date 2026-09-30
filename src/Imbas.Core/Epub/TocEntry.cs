namespace Imbas.Core.Epub;

/// <summary>An entry in the book's table of contents.</summary>
/// <param name="Title">The label shown to the reader.</param>
/// <param name="Path">The target document's path inside the archive, or null for a heading without a link.</param>
/// <param name="Fragment">The anchor within the target document, without the leading '#'.</param>
/// <param name="ChapterIndex">The index of the target in <see cref="EpubBook.Chapters"/>, or null if it is not in the reading order.</param>
/// <param name="Children">Nested entries.</param>
public sealed record TocEntry(
    string Title,
    string? Path,
    string? Fragment,
    int? ChapterIndex,
    IReadOnlyList<TocEntry> Children);
