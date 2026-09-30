using Imbas.Core.Epub;

namespace Imbas.Core.Tests.Epub;

public class ChapterHtmlTests
{
    private static readonly Dictionary<string, int> Chapters = new()
    {
        ["OEBPS/text/one.xhtml"] = 0,
        ["OEBPS/text/two.xhtml"] = 1,
    };

    private static string Render(string body, Func<string, byte[]?>? readResource = null) =>
        ChapterHtml.Render(TestEpub.Chapter("T", body), "OEBPS/text/one.xhtml", Chapters, readResource ?? (_ => null));

    [Fact]
    public void Render_ReturnsBodyContentWithoutTheBodyElement()
    {
        var html = Render("<p class=\"first\">Hello <em>there</em></p>");

        Assert.Contains("<p class=\"first\"", html);
        Assert.Contains("Hello <em>there</em></p>", html);
        Assert.DoesNotContain("<body", html);
        Assert.DoesNotContain("<title", html);
    }

    [Fact]
    public void Render_RemovesScriptsStylesAndEventHandlers()
    {
        var html = Render("""
            <script>alert(1)</script><style>body{}</style>
            <p onclick="alert(2)" ONMOUSEOVER="x()">Text</p>
            <a href="javascript:alert(3)">Bad</a>
            <iframe src="https://example.com"></iframe>
            """);

        Assert.DoesNotContain("alert", html);
        Assert.DoesNotContain("x()", html);
        Assert.DoesNotContain("iframe", html);
        Assert.DoesNotContain("body{}", html);
        Assert.Contains(">Text</p>", html);
    }

    [Fact]
    public void Render_InlinesImagesAsDataUris()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47];
        var html = Render(
            "<img src=\"../images/pic%201.png\" alt=\"Pic\"/><img src=\"missing.png\" alt=\"Gone\"/>",
            path => path == "OEBPS/images/pic 1.png" ? png : null);

        Assert.Contains($"src=\"data:image/png;base64,{Convert.ToBase64String(png)}\"", html);
        Assert.Contains("alt=\"Gone\"", html);
        Assert.DoesNotContain("missing.png", html);
    }

    [Fact]
    public void Render_RewritesLinksToOtherChapters()
    {
        var html = Render("""<a href="two.xhtml#sec">Next</a><a href="#local">Here</a>""");

        Assert.Contains($"href=\"#\" {ChapterHtml.ChapterAttribute}=\"1\" {ChapterHtml.FragmentAttribute}=\"sec\"", html);
        Assert.Contains($"{ChapterHtml.ChapterAttribute}=\"0\" {ChapterHtml.FragmentAttribute}=\"local\"", html);
    }

    [Fact]
    public void Render_LeavesExternalLinksAlone()
    {
        var html = Render("""<a href="https://example.com/x">Web</a>""");

        Assert.Contains("href=\"https://example.com/x\"", html);
        Assert.DoesNotContain(ChapterHtml.ChapterAttribute, html);
    }

    [Fact]
    public void Render_ClosesEmptyNonVoidElements()
    {
        var html = Render("<div id=\"a\"/><br/><p>After</p>");

        Assert.Contains("<div id=\"a\"></div>", html);
        Assert.Contains("<br />", html);
    }

    [Fact]
    public void Render_DropsXhtmlNamespacesButKeepsSvg()
    {
        var html = Render("""<section epub:type="chapter"><svg xmlns="http://www.w3.org/2000/svg"><rect/></svg></section>""");

        Assert.StartsWith("<section>", html);
        Assert.DoesNotContain("xhtml", html);
        Assert.DoesNotContain("epub", html);
        Assert.Contains("<svg xmlns=\"http://www.w3.org/2000/svg\">", html);
    }

    [Fact]
    public void ReadChapterHtml_RendersFromTheArchive()
    {
        using var book = EpubReader.Open(TestEpub.Epub3().ToStream());

        var html = book.ReadChapterHtml(1);

        Assert.Contains("<h1>Chapter One</h1>", html);
        Assert.Contains("dark and stormy", html);
    }
}
