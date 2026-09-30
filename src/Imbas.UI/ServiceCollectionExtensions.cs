using Imbas.Core;
using Imbas.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Imbas.UI;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the services the shared UI needs. Hosts also register an <see cref="IBookPicker"/>.</summary>
    /// <param name="dataDirectory">Where the app keeps its reading state and imported books.</param>
    public static IServiceCollection AddImbasUI(this IServiceCollection services, string dataDirectory)
    {
        services.AddSingleton<Library>();
        services.AddSingleton(_ => new ReadingStateStore(Path.Combine(dataDirectory, "reading-state.json")));
        services.AddSingleton(sp => new ReaderService(
            sp.GetRequiredService<Library>(),
            sp.GetRequiredService<ReadingStateStore>(),
            Path.Combine(dataDirectory, "Books")));
        return services;
    }
}
