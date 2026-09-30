namespace Imbas.Core;

/// <summary>Keeps copies of opened books in an app-owned folder.</summary>
/// <remarks>
/// File pickers on phones hand back temporary copies that disappear, so a book is copied into
/// the app's own folder before it is opened. That way it can be reopened on the next launch.
/// </remarks>
public static class BookFiles
{
    /// <summary>
    /// Copies a book into <paramref name="directory"/> and returns the copy's path. A book that
    /// is already in the folder is returned as is; a file with the same name is replaced.
    /// </summary>
    public static string Import(string sourcePath, string directory)
    {
        var source = Path.GetFullPath(sourcePath);
        var target = Path.GetFullPath(Path.Combine(directory, Path.GetFileName(source)));
        if (string.Equals(source, target, StringComparison.Ordinal))
        {
            return target;
        }

        Directory.CreateDirectory(directory);
        File.Copy(source, target, overwrite: true);
        return target;
    }
}
