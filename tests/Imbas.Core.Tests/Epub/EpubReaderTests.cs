using Imbas.Core.Epub;

namespace Imbas.Core.Tests.Epub;

public class EpubReaderTests
{
    [Fact]
    public void Open_ReadsEpub3Metadata()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        Assert.Equal("The Test Book", book.Title);
        Assert.Equal(["Ada Author", "Bob Writer"], book.Authors);
        Assert.Equal("en", book.Language);
        Assert.Equal("urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427", book.Identifier);
    }

    [Fact]
    public void Chapters_FollowSpineAndSkipMissingItems()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        Assert.Equal(["cover", "c1", "c2"], book.Chapters.Select(c => c.Id));
        Assert.Equal([0, 1, 2], book.Chapters.Select(c => c.Index));
        Assert.Equal("OEBPS/text/chapter 1.xhtml", book.Chapters[1].Path);
        Assert.False(book.Chapters[0].IsLinear);
        Assert.True(book.Chapters[1].IsLinear);
    }

    [Fact]
    public void Chapters_TakeTitlesFromTocNavNotLandmarks()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        Assert.Null(book.Chapters[0].Title);
        Assert.Equal("Section 1", book.Chapters[0].DisplayTitle);
        Assert.Equal("Chapter One", book.Chapters[1].Title);
        Assert.Equal("Chapter Two", book.Chapters[2].Title);
    }

    [Fact]
    public void TableOfContents_ReadsNestedEpub3Nav()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        var toc = book.TableOfContents;
        Assert.Equal(["Chapter One", "Appendices"], toc.Select(e => e.Title));

        var partB = Assert.Single(toc[0].Children);
        Assert.Equal("Part B", partB.Title);
        Assert.Equal("part-b", partB.Fragment);
        Assert.Equal(1, partB.ChapterIndex);

        Assert.Null(toc[1].Path);
        Assert.Null(toc[1].ChapterIndex);
        Assert.Equal(2, Assert.Single(toc[1].Children).ChapterIndex);
    }

    [Fact]
    public void TableOfContents_ReadsEpub2Ncx()
    {
        using var book = EpubReader.Open(TestEpub.Epub2().ToStream());

        Assert.Equal("Old Book", book.Title);
        Assert.Equal("old-book-1", book.Identifier);
        Assert.Empty(book.Authors);
        Assert.Equal(["First", "Second"], book.TableOfContents.Select(e => e.Title));
        Assert.Equal("two", Assert.Single(book.TableOfContents[0].Children).Fragment);
        Assert.Equal(["First", "Second"], book.Chapters.Select(c => c.Title));
    }

    [Fact]
    public void TableOfContents_LinksNcxEntriesToChapters()
    {
        var epub = TestEpub.Epub2();

        using var book = EpubReader.Open(epub.ToStream());

        Assert.Equal(0, book.TableOfContents[0].ChapterIndex);
        Assert.Equal(1, book.TableOfContents[1].ChapterIndex);
    }

    [Fact]
    public void ReadChapterXhtml_ReturnsTheRawDocument()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        Assert.Contains("stormy night", book.ReadChapterXhtml(1));
    }

    [Fact]
    public void ReadResource_ReadsByArchivePath()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        Assert.Equal("p { margin: 0 }"u8.ToArray(), book.ReadResource("OEBPS/style.css"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void ReadChapterHtml_RejectsIndexOutOfRange(int index)
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        Assert.Throws<ArgumentOutOfRangeException>(() => book.ReadChapterHtml(index));
    }

    [Fact]
    public void Open_ReadsFromDiskAndReleasesFileOnDispose()
    {
        var dir = Directory.CreateTempSubdirectory("imbas-tests-");
        try
        {
            var path = TestEpub.Epub3().WriteTo(dir.FullName);

            using (var book = EpubReader.Open(path))
            {
                Assert.Equal(3, book.Chapters.Count);
            }

            // Disposing the book releases the file.
            File.Delete(path);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Open_RejectsNonZipFiles()
    {
        var stream = new MemoryStream("not a zip"u8.ToArray());

        Assert.Throws<EpubFormatException>(() => EpubReader.Open(stream));
    }

    [Fact]
    public void Open_RejectsArchiveWithoutContainer()
    {
        var stream = new TestEpub().With("hello.txt", "hi").ToStream();

        var ex = Assert.Throws<EpubFormatException>(() => EpubReader.Open(stream));
        Assert.Contains("container.xml", ex.Message);
    }

    [Fact]
    public void Open_RejectsMissingPackageDocument()
    {
        var stream = new TestEpub().With("META-INF/container.xml", TestEpub.Container("nope.opf")).ToStream();

        Assert.Throws<EpubFormatException>(() => EpubReader.Open(stream));
    }

    [Fact]
    public void Open_RejectsMalformedXml()
    {
        var stream = new TestEpub()
            .With("META-INF/container.xml", TestEpub.Container("content.opf"))
            .With("content.opf", "<package><metadata>")
            .ToStream();

        Assert.Throws<EpubFormatException>(() => EpubReader.Open(stream));
    }

    [Fact]
    public void Open_ListsChaptersWhenBookHasNoToc()
    {
        var stream = new TestEpub()
            .With("META-INF/container.xml", TestEpub.Container("content.opf"))
            .With("content.opf", """
                <package xmlns="http://www.idpf.org/2007/opf" version="3.0">
                  <metadata/>
                  <manifest><item id="a" href="a.xhtml" media-type="application/xhtml+xml"/></manifest>
                  <spine><itemref idref="a"/></spine>
                </package>
                """)
            .With("a.xhtml", TestEpub.Chapter("A", "<p>Alpha</p>"))
            .ToStream();

        using var book = EpubReader.Open(stream);

        Assert.Equal("Untitled", book.Title);
        Assert.Null(book.Identifier);
        Assert.Empty(book.TableOfContents);
        Assert.Equal("Section 1", Assert.Single(book.Chapters).DisplayTitle);
    }
}
