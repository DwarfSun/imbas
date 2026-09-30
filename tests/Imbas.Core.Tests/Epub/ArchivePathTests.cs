using Imbas.Core.Epub;

namespace Imbas.Core.Tests.Epub;

public class ArchivePathTests
{
    [Theory]
    [InlineData("OEBPS/", "text/ch1.xhtml", "OEBPS/text/ch1.xhtml", null)]
    [InlineData("OEBPS/text/", "../images/a.png", "OEBPS/images/a.png", null)]
    [InlineData("OEBPS/", "./ch%201.xhtml#sec%202", "OEBPS/ch 1.xhtml", "sec 2")]
    [InlineData("", "ch1.xhtml#", "ch1.xhtml", null)]
    [InlineData("OEBPS/", "/root.xhtml", "root.xhtml", null)]
    [InlineData("a/", "../../../x.xhtml", "x.xhtml", null)]
    public void Resolve_HandlesRelativeReferences(string baseDir, string href, string path, string? fragment)
    {
        Assert.Equal((path, fragment), ArchivePath.Resolve(baseDir, href));
    }

    [Theory]
    [InlineData("content.opf", "")]
    [InlineData("OEBPS/content.opf", "OEBPS/")]
    public void Directory_ReturnsParentWithTrailingSlash(string path, string directory)
    {
        Assert.Equal(directory, ArchivePath.Directory(path));
    }
}
