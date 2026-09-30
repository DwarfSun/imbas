namespace Imbas.Core;

/// <summary>
/// The file formats Imbas knows about.
/// </summary>
public enum BookFormat
{
    Unknown,
    Epub,
    Pdf,
    PlainText,
}

public static class BookFormatExtensions
{
    /// <summary>
    /// Infers a book's format from its file extension.
    /// </summary>
    public static BookFormat FromPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".epub" => BookFormat.Epub,
            ".pdf" => BookFormat.Pdf,
            ".txt" => BookFormat.PlainText,
            _ => BookFormat.Unknown,
        };
    }
}
