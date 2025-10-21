using Google.Apis.YouTube.v3;

namespace YoutubePlaylistManager.Cli.Options;

public class PlaylistManagerOptions
{
    public required string ApiKey { get; set; }
    public required string ChannelID { get; set; }
}
