namespace Imbas.Core;

/// <summary>
/// Where a reader is in a book: a chapter (or page, for fixed-layout formats)
/// and how far through it they are, from 0 to 1.
/// </summary>
public readonly record struct ReadingPosition
{
    public ReadingPosition(int chapterIndex, double chapterProgress)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(chapterIndex);
        if (double.IsNaN(chapterProgress) || chapterProgress < 0 || chapterProgress > 1)
            throw new ArgumentOutOfRangeException(nameof(chapterProgress), chapterProgress, "Progress must be between 0 and 1.");

        ChapterIndex = chapterIndex;
        ChapterProgress = chapterProgress;
    }

    public int ChapterIndex { get; }

    public double ChapterProgress { get; }

    /// <summary>
    /// The very beginning of a book.
    /// </summary>
    public static ReadingPosition Start => new(0, 0);
}
