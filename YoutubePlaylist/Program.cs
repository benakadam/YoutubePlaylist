using Microsoft.Extensions.DependencyInjection;
using YoutubePlaylist.DI;

namespace YoutubePlaylist;

public static class Program
{
    [STAThread]
    public static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddYoutubePlaylistServices();
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<YoutubePlaylist>();
        await app.StartProcess();
    }
}
