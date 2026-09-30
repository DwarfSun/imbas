using Imbas.Core;
using Imbas.UI;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace Imbas.Linux;

// Hosts the shared Blazor UI in a native window via Photino, since .NET MAUI's
// BlazorWebView has no Linux backend.
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
        builder.Services.AddSingleton<Library>();
        builder.RootComponents.Add<Routes>("#app");

        var app = builder.Build();
        app.MainWindow
            .SetTitle("Imbas")
            .SetSize(1024, 768);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            app.MainWindow.ShowMessage("Imbas", e.ExceptionObject.ToString());

        app.Run();
    }
}
