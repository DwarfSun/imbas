namespace Imbas.Core.Epub;

/// <summary>Resolves the relative, URL-encoded references inside an EPUB to archive entry paths.</summary>
internal static class ArchivePath
{
    /// <summary>The directory part of an archive path, with a trailing '/', or "" at the root.</summary>
    public static string Directory(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? "" : path[..(slash + 1)];
    }

    /// <summary>Resolves <paramref name="href"/> against <paramref name="baseDirectory"/>.</summary>
    /// <returns>The archive path, and the fragment without its '#' (or null).</returns>
    public static (string Path, string? Fragment) Resolve(string baseDirectory, string href)
    {
        string? fragment = null;
        var hash = href.IndexOf('#');
        if (hash >= 0)
        {
            fragment = hash + 1 < href.Length ? Uri.UnescapeDataString(href[(hash + 1)..]) : null;
            href = href[..hash];
        }

        href = Uri.UnescapeDataString(href);
        if (href.Length == 0)
        {
            return ("", fragment);
        }

        var combined = href.StartsWith('/') ? href[1..] : baseDirectory + href;
        var segments = new List<string>();
        foreach (var segment in combined.Split('/'))
        {
            if (segment is "" or ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            segments.Add(segment);
        }

        return (string.Join('/', segments), fragment);
    }
}
