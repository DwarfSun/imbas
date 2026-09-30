namespace Imbas.Core.Tests;

public class ReadingPositionTests
{
    [Fact]
    public void Start_IsFirstChapterAtZero()
    {
        Assert.Equal(new ReadingPosition(0, 0), ReadingPosition.Start);
    }

    [Theory]
    [InlineData(-1, 0.5)]
    [InlineData(0, -0.1)]
    [InlineData(0, 1.1)]
    [InlineData(0, double.NaN)]
    public void Constructor_RejectsOutOfRangeValues(int chapter, double progress)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadingPosition(chapter, progress));
    }
}
