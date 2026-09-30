using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Imbas.Core.Epub;

internal static partial class Xml
{
    private static readonly HashSet<string> XmlEntities = ["amp", "lt", "gt", "quot", "apos"];

    /// <summary>
    /// Parses an EPUB XML or XHTML document. HTML named entities such as &amp;nbsp; are common in
    /// real books but undefined in XML, so they are replaced with their characters before parsing.
    /// </summary>
    public static XDocument Parse(string text)
    {
        text = NamedEntity().Replace(text, m =>
            XmlEntities.Contains(m.Groups[1].Value) ? m.Value : WebUtility.HtmlDecode(m.Value));

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
        };

        using var reader = XmlReader.Create(new StringReader(text), settings);
        return XDocument.Load(reader);
    }

    public static IEnumerable<XElement> ElementsNamed(this XContainer container, string localName) =>
        container.Elements().Where(e => e.Name.LocalName == localName);

    public static XElement? ElementNamed(this XContainer container, string localName) =>
        container.ElementsNamed(localName).FirstOrDefault();

    public static IEnumerable<XElement> DescendantsNamed(this XContainer container, string localName) =>
        container.Descendants().Where(e => e.Name.LocalName == localName);

    public static string? AttributeValue(this XElement element, string localName) =>
        element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value;

    [GeneratedRegex(@"&([A-Za-z][A-Za-z0-9]*);")]
    private static partial Regex NamedEntity();
}
