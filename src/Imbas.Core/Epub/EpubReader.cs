using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Imbas.Core.Epub;

/// <summary>Opens EPUB 2 and EPUB 3 files.</summary>
public static class EpubReader
{
    private const string NcxMediaType = "application/x-dtbncx+xml";

    public static EpubBook Open(string path)
    {
        var stream = File.OpenRead(path);
        try
        {
            return Open(stream, leaveOpen: false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <param name="stream">A readable, seekable stream holding the EPUB archive.</param>
    /// <param name="leaveOpen">Whether disposing the book should leave <paramref name="stream"/> open.</param>
    public static EpubBook Open(Stream stream, bool leaveOpen = false)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen);
        }
        catch (InvalidDataException ex)
        {
            throw new EpubFormatException("The file is not a ZIP archive.", ex);
        }

        try
        {
            return Load(archive);
        }
        catch (XmlException ex)
        {
            archive.Dispose();
            throw new EpubFormatException("The book contains malformed XML.", ex);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    private static EpubBook Load(ZipArchive archive)
    {
        var packagePath = FindPackagePath(archive);
        var package = ReadXml(archive, packagePath).Root
            ?? throw new EpubFormatException("The package document is empty.");
        var packageDir = ArchivePath.Directory(packagePath);

        var metadata = package.ElementNamed("metadata");
        var manifest = ReadManifest(package, packageDir);
        var spine = package.ElementNamed("spine") ?? throw new EpubFormatException("The package has no spine.");

        var spineItems = spine.ElementsNamed("itemref")
            .Select(itemref => (Ref: itemref, Item: manifest.GetValueOrDefault(itemref.AttributeValue("idref") ?? "")))
            .Where(x => x.Item is not null)
            .ToList();

        var chapterIndexByPath = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < spineItems.Count; i++)
        {
            chapterIndexByPath.TryAdd(spineItems[i].Item!.Path, i);
        }

        var toc = ReadTableOfContents(archive, manifest, spine, chapterIndexByPath);
        var titles = new Dictionary<int, string>();
        CollectTitles(toc, titles);

        var chapters = spineItems
            .Select((x, i) => new EpubChapter(
                i,
                x.Item!.Id,
                x.Item.Path,
                titles.GetValueOrDefault(i),
                x.Ref.AttributeValue("linear") != "no"))
            .ToList();

        return new EpubBook(
            archive,
            identifier: ReadIdentifier(package, metadata),
            title: Text(metadata?.ElementNamed("title")) ?? "Untitled",
            authors: metadata?.ElementsNamed("creator").Select(Text).OfType<string>().ToList() ?? [],
            language: Text(metadata?.ElementNamed("language")),
            chapters,
            toc);
    }

    private static string FindPackagePath(ZipArchive archive)
    {
        if (archive.GetEntry("META-INF/container.xml") is null)
        {
            throw new EpubFormatException("The file has no META-INF/container.xml, so it is not an EPUB.");
        }

        var container = ReadXml(archive, "META-INF/container.xml");
        var rootfile = container.DescendantsNamed("rootfile")
            .FirstOrDefault(r => r.AttributeValue("media-type") is null or "application/oebps-package+xml");
        var path = rootfile?.AttributeValue("full-path");
        if (string.IsNullOrEmpty(path))
        {
            throw new EpubFormatException("container.xml does not name a package document.");
        }

        return path;
    }

    private static Dictionary<string, ManifestItem> ReadManifest(XElement package, string packageDir)
    {
        var items = new Dictionary<string, ManifestItem>(StringComparer.Ordinal);
        foreach (var item in package.ElementNamed("manifest")?.ElementsNamed("item") ?? [])
        {
            var id = item.AttributeValue("id");
            var href = item.AttributeValue("href");
            if (id is null || href is null)
            {
                continue;
            }

            var (path, _) = ArchivePath.Resolve(packageDir, href);
            items.TryAdd(id, new ManifestItem(
                id,
                path,
                item.AttributeValue("media-type") ?? "",
                (item.AttributeValue("properties") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)));
        }

        return items;
    }

    private static string? ReadIdentifier(XElement package, XElement? metadata)
    {
        var identifiers = metadata?.ElementsNamed("identifier").ToList() ?? [];
        var uniqueId = package.AttributeValue("unique-identifier");
        var unique = identifiers.FirstOrDefault(e => uniqueId is not null && e.AttributeValue("id") == uniqueId);
        return Text(unique ?? identifiers.FirstOrDefault());
    }

    private static IReadOnlyList<TocEntry> ReadTableOfContents(
        ZipArchive archive,
        Dictionary<string, ManifestItem> manifest,
        XElement spine,
        Dictionary<string, int> chapterIndexByPath)
    {
        // EPUB 3 books declare an XHTML navigation document; EPUB 2 books use an NCX file.
        var nav = manifest.Values.FirstOrDefault(i => i.Properties.Contains("nav"));
        if (nav is not null && archive.GetEntry(nav.Path) is not null)
        {
            var entries = NavParser.ParseNav(ReadXml(archive, nav.Path), nav.Path, chapterIndexByPath);
            if (entries.Count > 0)
            {
                return entries;
            }
        }

        var ncx = manifest.GetValueOrDefault(spine.AttributeValue("toc") ?? "")
            ?? manifest.Values.FirstOrDefault(i => i.MediaType == NcxMediaType);
        if (ncx is not null && archive.GetEntry(ncx.Path) is not null)
        {
            return NavParser.ParseNcx(ReadXml(archive, ncx.Path), ncx.Path, chapterIndexByPath);
        }

        return [];
    }

    private static void CollectTitles(IEnumerable<TocEntry> entries, Dictionary<int, string> titles)
    {
        foreach (var entry in entries)
        {
            // The first entry pointing at a document names it; later ones usually point at sections within it.
            if (entry.ChapterIndex is { } index)
            {
                titles.TryAdd(index, entry.Title);
            }

            CollectTitles(entry.Children, titles);
        }
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new EpubFormatException($"The book is missing '{path}'.");
        using var reader = new StreamReader(entry.Open(), detectEncodingFromByteOrderMarks: true);
        return Xml.Parse(reader.ReadToEnd());
    }

    private static string? Text(XElement? element)
    {
        var value = element?.Value.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private sealed record ManifestItem(string Id, string Path, string MediaType, string[] Properties);
}
