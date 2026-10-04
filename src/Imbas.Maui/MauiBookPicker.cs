using Imbas.UI.Services;

namespace Imbas.Maui;

/// <summary>Picks books with the platform file picker.</summary>
public sealed class MauiBookPicker : IBookPicker
{
	private static readonly FilePickerFileType EpubFiles = new(new Dictionary<DevicePlatform, IEnumerable<string>>
	{
		[DevicePlatform.Android] = ["application/epub+zip"],
		[DevicePlatform.iOS] = ["org.idpf.epub-container"],
		[DevicePlatform.MacCatalyst] = ["org.idpf.epub-container"],
		[DevicePlatform.WinUI] = [".epub"],
	});

	public async Task<string?> PickBookAsync()
	{
		// On phones the result is a temporary copy; ReaderService imports it into app storage.
		var result = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = "Open book",
			FileTypes = EpubFiles,
		});

		return result?.FullPath;
	}
}
