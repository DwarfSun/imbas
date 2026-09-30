# imbas
Open source, cross-platform e-reader

## Layout

- `src/Imbas.Core` — the book model, library and reading position, with no UI dependencies.
- `src/Imbas.UI` — the Blazor components that make up the interface, shared by every host.
- `src/Imbas.Maui` — the .NET MAUI Blazor Hybrid app for Windows, macOS, iOS and Android.
- `src/Imbas.Linux` — a [Photino](https://www.tryphotino.io/) host for Linux, since MAUI's BlazorWebView doesn't run there.
- `tests/Imbas.Core.Tests` — xUnit tests for the core library.

## Building

Requires the .NET 10 SDK.

```sh
dotnet test tests/Imbas.Core.Tests
```

Linux app (needs `libwebkit2gtk-4.1-0` and `libnotify4` at runtime):

```sh
dotnet run --project src/Imbas.Linux
```

MAUI app (needs the MAUI workloads for your target, e.g. `dotnet workload install maui-windows`):

```sh
dotnet build src/Imbas.Maui -f net10.0-windows10.0.19041.0
```
