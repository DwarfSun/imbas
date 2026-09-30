using System.IO.Compression;
using System.Text;

namespace Imbas.Core.Tests;

/// <summary>Builds small EPUB archives in memory so tests need no binary fixtures.</summary>
internal sealed class TestEpub
{
    private readonly Dictionary<string, string> _files = new();

    public TestEpub With(string path, string content)
    {
        _files[path] = content;
        return this;
    }

    public MemoryStream ToStream()
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // The mimetype entry comes first and uncompressed, as the spec requires.
            Add(zip, "mimetype", "application/epub+zip", CompressionLevel.NoCompression);
            foreach (var (path, content) in _files)
            {
                Add(zip, path, content, CompressionLevel.Optimal);
            }
        }

        stream.Position = 0;
        return stream;
    }

    public string WriteTo(string directory, string name = "book.epub")
    {
        var path = Path.Combine(directory, name);
        using var file = File.Create(path);
        ToStream().CopyTo(file);
        return path;
    }

    public static string Container(string packagePath = "OEBPS/content.opf") => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
          <rootfiles>
            <rootfile full-path="{packagePath}" media-type="application/oebps-package+xml"/>
          </rootfiles>
        </container>
        """;

    public static string Chapter(string title, string body) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>{title}</title></head>
          <body>{body}</body>
        </html>
        """;

    /// <summary>An EPUB 3 book with a navigation document and three chapters, one of them nested in a folder.</summary>
    public static TestEpub Epub3() => new TestEpub()
        .With("META-INF/container.xml", Container())
        .With("OEBPS/content.opf", """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="bookid">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                <dc:identifier id="isbn">urn:isbn:9780000000000</dc:identifier>
                <dc:identifier id="bookid">urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427</dc:identifier>
                <dc:title>  The Test Book </dc:title>
                <dc:creator>Ada Author</dc:creator>
                <dc:creator>Bob Writer</dc:creator>
                <dc:language>en</dc:language>
              </metadata>
              <manifest>
                <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>
                <item id="cover" href="cover.xhtml" media-type="application/xhtml+xml"/>
                <item id="c1" href="text/chapter%201.xhtml" media-type="application/xhtml+xml"/>
                <item id="c2" href="text/chapter2.xhtml" media-type="application/xhtml+xml"/>
                <item id="css" href="style.css" media-type="text/css"/>
              </manifest>
              <spine>
                <itemref idref="cover" linear="no"/>
                <itemref idref="c1"/>
                <itemref idref="missing"/>
                <itemref idref="c2"/>
              </spine>
            </package>
            """)
        .With("OEBPS/nav.xhtml", """
            <?xml version="1.0" encoding="UTF-8"?>
            <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
              <body>
                <nav epub:type="landmarks"><ol><li><a href="cover.xhtml">Cover landmark</a></li></ol></nav>
                <nav epub:type="toc">
                  <ol>
                    <li><a href="text/chapter%201.xhtml">Chapter
                        One</a>
                      <ol>
                        <li><a href="text/chapter%201.xhtml#part-b">Part B</a></li>
                      </ol>
                    </li>
                    <li><span>Appendices</span>
                      <ol><li><a href="text/chapter2.xhtml">Chapter Two</a></li></ol>
                    </li>
                  </ol>
                </nav>
              </body>
            </html>
            """)
        .With("OEBPS/cover.xhtml", Chapter("Cover", "<p>Cover page</p>"))
        .With("OEBPS/text/chapter 1.xhtml", Chapter("One", "<h1>Chapter One</h1><p>It was a dark&nbsp;and stormy night.</p><p id=\"part-b\">Part B text.</p>"))
        .With("OEBPS/text/chapter2.xhtml", Chapter("Two", "<h2>Chapter Two</h2><p>The end.</p>"))
        .With("OEBPS/style.css", "p { margin: 0 }");

    /// <summary>An EPUB 2 book whose table of contents is an NCX file.</summary>
    public static TestEpub Epub2() => new TestEpub()
        .With("META-INF/container.xml", Container("content.opf"))
        .With("content.opf", """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="uid">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:opf="http://www.idpf.org/2007/opf">
                <dc:title>Old Book</dc:title>
                <dc:identifier id="uid">old-book-1</dc:identifier>
              </metadata>
              <manifest>
                <item id="ncx" href="toc.ncx" media-type="application/x-dtbncx+xml"/>
                <item id="a" href="a.html" media-type="application/xhtml+xml"/>
                <item id="b" href="b.html" media-type="application/xhtml+xml"/>
              </manifest>
              <spine toc="ncx">
                <itemref idref="a"/>
                <itemref idref="b"/>
              </spine>
            </package>
            """)
        .With("toc.ncx", """
            <?xml version="1.0" encoding="UTF-8"?>
            <ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1">
              <navMap>
                <navPoint id="p1" playOrder="1">
                  <navLabel><text>First</text></navLabel>
                  <content src="a.html"/>
                  <navPoint id="p1a" playOrder="2">
                    <navLabel><text>First, part two</text></navLabel>
                    <content src="a.html#two"/>
                  </navPoint>
                </navPoint>
                <navPoint id="p2" playOrder="3">
                  <navLabel><text>Second</text></navLabel>
                  <content src="b.html"/>
                </navPoint>
              </navMap>
            </ncx>
            """)
        .With("a.html", Chapter("A", "<p>Alpha</p>"))
        .With("b.html", Chapter("B", "<p>Beta</p>"));

    private static void Add(ZipArchive zip, string path, string content, CompressionLevel level)
    {
        var entry = zip.CreateEntry(path, level);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
