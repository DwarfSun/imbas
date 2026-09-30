namespace Imbas.Core.Epub;

/// <summary>Thrown when a file is not a readable EPUB.</summary>
public sealed class EpubFormatException : Exception
{
    public EpubFormatException(string message) : base(message) { }

    public EpubFormatException(string message, Exception inner) : base(message, inner) { }
}
