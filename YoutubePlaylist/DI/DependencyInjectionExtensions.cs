using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.DependencyInjection;
using YoutubePlaylistManager.Cli.Helpers;
using YoutubePlaylistManager.Cli.Interface;
using YoutubePlaylistManager.Cli.Manager;
using YoutubePlaylistManager.Cli.Options;

namespace YoutubePlaylistManager.Cli.DI;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddYoutubePlaylistServices(this IServiceCollection services)
    {
        // Register options
        services.Configure<YoutubePlaylistOptions>(options =>
        {
            options.DownloadPath = Helper.GetConfigValue("DownloadPath");
            options.PlaylistBaseUrl = Helper.GetConfigValue("PlaylistBaseUrl");
        });

        services.Configure<DownloadManagerOptions>(options =>
        {
            options.DownloadPath = Helper.GetConfigValue("DownloadPath");
        });

        services.Configure<PlaylistManagerOptions>(options =>
        {
            options.ApiKey = Helper.GetConfigValue("ApiKey");
            options.ChannelID = Helper.GetConfigValue("ChannelID");
        });

        // Register services
        services.AddSingleton(provider =>
        {
            var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlaylistManagerOptions>>().Value;
            return new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = opts.ApiKey,
                ApplicationName = "YouTubePlaylist"
            });
        });

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IDataAccess, DataAccess.DataAccess>();
        services.AddSingleton<YoutubeApiManager>();
        services.AddSingleton<DownloadManager>();
        services.AddSingleton<PlaylistManagerService>();
        return services;
    }
}
