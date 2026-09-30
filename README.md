# imbas
Open source, cross-platform e-reader

## Layout

- `src/Imbas.App` — the desktop app, built with [Avalonia](https://avaloniaui.net/).
- `src/Imbas.Core` — the book model, library and reading position, with no UI dependencies.
- `tests/Imbas.Core.Tests` — xUnit tests for the core library.

## Building

Requires the .NET 10 SDK.

```sh
dotnet build
dotnet test
dotnet run --project src/Imbas.App
```
