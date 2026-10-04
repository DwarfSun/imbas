namespace Imbas.UI.Services;

/// <summary>Shows the platform's file picker. Each host (MAUI, Photino) supplies its own.</summary>
public interface IBookPicker
{
    /// <summary>Asks the reader to choose an EPUB and returns a local path to it, or null if they cancelled.</summary>
    Task<string?> PickBookAsync();
}
