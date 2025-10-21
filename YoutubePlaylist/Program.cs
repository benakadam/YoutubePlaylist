using Microsoft.Extensions.DependencyInjection;
using YoutubePlaylistManager.Cli.DI;

namespace YoutubePlaylistManager.Cli;

public static class Program
{
    [STAThread]
    public static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddYoutubePlaylistServices();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<PlaylistManagerService>();
        await app.StartProcess();
    }
}
