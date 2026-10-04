using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Imbas.Core.Epub;

/// <summary>Reads tables of contents from EPUB 3 navigation documents and EPUB 2 NCX files.</summary>
internal static partial class NavParser
{
    private const string OpsNamespace = "http://www.idpf.org/2007/ops";

    public static IReadOnlyList<TocEntry> ParseNav(XDocument document, string navPath, IReadOnlyDictionary<string, int> chapters)
    {
        var navs = document.DescendantsNamed("nav").ToList();
        var toc = navs.FirstOrDefault(n => HasTocType(n)) ?? navs.FirstOrDefault();
        var list = toc?.ElementNamed("ol");
        return list is null ? [] : ParseNavList(list, ArchivePath.Directory(navPath), chapters);
    }

    public static IReadOnlyList<TocEntry> ParseNcx(XDocument document, string ncxPath, IReadOnlyDictionary<string, int> chapters)
    {
        var navMap = document.DescendantsNamed("navMap").FirstOrDefault();
        return navMap is null ? [] : ParseNavPoints(navMap, ArchivePath.Directory(ncxPath), chapters);
    }

    private static bool HasTocType(XElement nav)
    {
        var type = nav.Attribute(XName.Get("type", OpsNamespace))?.Value ?? nav.AttributeValue("type");
        return type?.Split(' ').Contains("toc") == true;
    }

    private static List<TocEntry> ParseNavList(XElement list, string baseDir, IReadOnlyDictionary<string, int> chapters)
    {
        var entries = new List<TocEntry>();
        foreach (var item in list.ElementsNamed("li"))
        {
            var label = item.ElementNamed("a") ?? item.ElementNamed("span");
            var children = item.ElementNamed("ol") is { } nested ? ParseNavList(nested, baseDir, chapters) : [];
            var title = Clean(label?.Value);
            if (title is null)
            {
                // A nameless item carries no information of its own; keep its children.
                entries.AddRange(children);
                continue;
            }

            entries.Add(CreateEntry(title, label?.AttributeValue("href"), baseDir, chapters, children));
        }

        return entries;
    }

    private static List<TocEntry> ParseNavPoints(XElement parent, string baseDir, IReadOnlyDictionary<string, int> chapters)
    {
        var entries = new List<TocEntry>();
        foreach (var point in parent.ElementsNamed("navPoint"))
        {
            var children = ParseNavPoints(point, baseDir, chapters);
            var title = Clean(point.ElementNamed("navLabel")?.ElementNamed("text")?.Value);
            if (title is null)
            {
                entries.AddRange(children);
                continue;
            }

            entries.Add(CreateEntry(title, point.ElementNamed("content")?.AttributeValue("src"), baseDir, chapters, children));
        }

        return entries;
    }

    private static TocEntry CreateEntry(
        string title,
        string? href,
        string baseDir,
        IReadOnlyDictionary<string, int> chapters,
        IReadOnlyList<TocEntry> children)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return new TocEntry(title, null, null, null, children);
        }

        var (path, fragment) = ArchivePath.Resolve(baseDir, href);
        int? index = chapters.TryGetValue(path, out var i) ? i : null;
        return new TocEntry(title, path, fragment, index, children);
    }

    private static string? Clean(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var collapsed = Whitespace().Replace(text, " ").Trim();
        return collapsed.Length == 0 ? null : collapsed;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
