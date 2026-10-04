using System.Xml.Linq;

namespace Imbas.Core.Epub;

/// <summary>
/// Turns a chapter's XHTML into an HTML fragment that is safe to embed in the app's web view:
/// scripts and event handlers are removed, images are inlined as data URIs, and links to other
/// documents in the book are rewritten so the reader can handle them.
/// </summary>
public static class ChapterHtml
{
    /// <summary>Links inside the book point at this attribute's chapter index instead of a URL.</summary>
    public const string ChapterAttribute = "data-imbas-chapter";

    /// <summary>The anchor within the target chapter, if the link has one.</summary>
    public const string FragmentAttribute = "data-imbas-fragment";

    private static readonly XNamespace Xhtml = "http://www.w3.org/1999/xhtml";
    private static readonly XNamespace Ops = "http://www.idpf.org/2007/ops";

    private static readonly HashSet<string> Removed =
        ["script", "style", "noscript", "iframe", "object", "embed", "form", "input", "button", "link", "meta", "base"];

    private static readonly HashSet<string> Void =
        ["area", "br", "col", "hr", "img", "wbr", "source", "track"];

    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
    };

    /// <summary>Returns the inner HTML of a chapter's body, cleaned for display.</summary>
    /// <param name="xhtml">The chapter document.</param>
    /// <param name="chapterPath">The chapter's archive path, used to resolve relative references.</param>
    /// <param name="chapterIndexByPath">The reading order, to turn links into chapter indexes.</param>
    /// <param name="readResource">Reads an archive entry, or returns null if it is missing.</param>
    public static string Render(
        string xhtml,
        string chapterPath,
        IReadOnlyDictionary<string, int> chapterIndexByPath,
        Func<string, byte[]?> readResource)
    {
        var document = Xml.Parse(xhtml);
        var body = document.Root?.DescendantsNamed("body").FirstOrDefault() ?? document.Root;
        if (body is null)
        {
            return "";
        }

        var baseDir = ArchivePath.Directory(chapterPath);

        foreach (var element in body.Descendants().ToList())
        {
            var name = element.Name.LocalName.ToLowerInvariant();
            if (Removed.Contains(name))
            {
                element.Remove();
                continue;
            }

            foreach (var attribute in element.Attributes().ToList())
            {
                if (ShouldDrop(attribute))
                {
                    attribute.Remove();
                }
            }

            switch (name)
            {
                case "img":
                    InlineImage(element, "src", baseDir, readResource);
                    break;
                case "image":
                    // SVG images use href or xlink:href.
                    InlineImage(element, "href", baseDir, readResource);
                    break;
                case "a":
                    RewriteLink(element, baseDir, chapterPath, chapterIndexByPath);
                    break;
            }

            // Drop the XHTML namespace so elements serialize as plain HTML tags without xmlns noise.
            if (element.Name.Namespace == Xhtml)
            {
                element.Name = element.Name.LocalName;
            }

            // HTML parsers read <div/> as an unclosed tag, so give every non-void element an end tag.
            if (element.IsEmpty && !Void.Contains(name))
            {
                element.Value = "";
            }
        }

        return string.Concat(body.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));
    }

    private static bool ShouldDrop(XAttribute attribute)
    {
        // Namespace declarations are regenerated as needed when serializing; epub:type means nothing to a browser.
        if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace == Ops)
        {
            return true;
        }

        var name = attribute.Name.LocalName;
        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return name is "href" or "src" or "action" or "formaction"
            && attribute.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);
    }

    private static void InlineImage(XElement element, string localName, string baseDir, Func<string, byte[]?> readResource)
    {
        var attribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName);
        if (attribute is null || IsExternal(attribute.Value))
        {
            return;
        }

        var (path, _) = ArchivePath.Resolve(baseDir, attribute.Value);
        var bytes = path.Length > 0 ? readResource(path) : null;
        if (bytes is null || !ImageTypes.TryGetValue(Path.GetExtension(path), out var mediaType))
        {
            attribute.Remove();
            return;
        }

        attribute.Value = $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static void RewriteLink(
        XElement link,
        string baseDir,
        string chapterPath,
        IReadOnlyDictionary<string, int> chapterIndexByPath)
    {
        var href = link.Attribute("href");
        if (href is null || IsExternal(href.Value))
        {
            return;
        }

        var (path, fragment) = ArchivePath.Resolve(baseDir, href.Value);
        if (path.Length == 0)
        {
            path = chapterPath;
        }

        href.Value = "#";
        if (chapterIndexByPath.TryGetValue(path, out var index))
        {
            link.SetAttributeValue(ChapterAttribute, index);
            link.SetAttributeValue(FragmentAttribute, fragment);
        }
    }

    private static bool IsExternal(string reference) =>
        reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
        || (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto");
}
