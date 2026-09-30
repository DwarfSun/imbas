using System.Text.Json;
using System.Text.Json.Serialization;
using Imbas.Core.Epub;

namespace Imbas.Core;

/// <summary>
/// Persists which book was open last and where the reader stopped in each book, as a small JSON
/// file. A missing or unreadable file is treated as empty, so a damaged file never blocks reading.
/// </summary>
public sealed class ReadingStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private State _state;

    public ReadingStateStore(string filePath)
    {
        _filePath = filePath;
        _state = Load(filePath);
    }

    /// <summary>The path of the book that was open when the app last saved, if any.</summary>
    public string? LastOpenedPath => _state.LastOpenedPath;

    /// <summary>A stable key for a book: its package identifier, or its file path when it has none.</summary>
    public static string KeyFor(EpubBook book, string filePath) =>
        book.Identifier is { Length: > 0 } id ? "id:" + id : "path:" + Path.GetFullPath(filePath);

    /// <summary>Returns where the reader left off in a book, or null if nothing valid was saved.</summary>
    public ReadingPosition? GetPosition(string bookKey)
    {
        if (!_state.Positions.TryGetValue(bookKey, out var saved))
        {
            return null;
        }

        try
        {
            return new ReadingPosition(saved.Chapter, saved.Progress);
        }
        catch (ArgumentOutOfRangeException)
        {
            // A hand-edited or damaged file; fall back to the start of the book.
            return null;
        }
    }

    /// <summary>Records the position in a book and marks it as the last opened book, then writes the file.</summary>
    public void Save(string bookKey, string filePath, ReadingPosition position)
    {
        _state.Positions[bookKey] = new SavedPosition(position.ChapterIndex, position.ChapterProgress);
        _state.LastOpenedPath = Path.GetFullPath(filePath);
        Write();
    }

    /// <summary>Forgets the last opened book, for example when its file has gone.</summary>
    public void ClearLastOpened()
    {
        _state.LastOpenedPath = null;
        Write();
    }

    private void Write()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file and move it into place so a crash never leaves half a file.
        var temp = _filePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_state, JsonOptions));
        File.Move(temp, _filePath, overwrite: true);
    }

    private static State Load(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var state = JsonSerializer.Deserialize<State>(File.ReadAllText(filePath), JsonOptions);
                if (state is not null)
                {
                    state.Positions ??= new(StringComparer.Ordinal);
                    return state;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
        }

        return new State();
    }

    private sealed class State
    {
        [JsonPropertyName("lastOpenedPath")]
        public string? LastOpenedPath { get; set; }

        [JsonPropertyName("positions")]
        public Dictionary<string, SavedPosition> Positions { get; set; } = new(StringComparer.Ordinal);
    }

    private sealed record SavedPosition(
        [property: JsonPropertyName("chapter")] int Chapter,
        [property: JsonPropertyName("progress")] double Progress);
}
