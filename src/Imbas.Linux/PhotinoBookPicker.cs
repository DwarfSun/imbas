using Imbas.UI.Services;
using Photino.NET;

namespace Imbas.Linux;

/// <summary>Picks books with the native file dialog of the Photino window.</summary>
internal sealed class PhotinoBookPicker : IBookPicker
{
    /// <summary>Set once the app is built, since the window doesn't exist while services are registered.</summary>
    public PhotinoWindow? Window { get; set; }

    public async Task<string?> PickBookAsync()
    {
        if (Window is null)
            return null;

        var paths = await Window.ShowOpenFileAsync("Open book", filters: [("EPUB books", ["*.epub"])]);
        return paths?.FirstOrDefault();
    }
}
