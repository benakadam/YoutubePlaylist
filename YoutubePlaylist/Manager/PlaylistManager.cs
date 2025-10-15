using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YoutubePlaylist.Interface;
using Microsoft.Extensions.Options;
using YoutubePlaylist.Options;

namespace YoutubePlaylist.Manager;
public class PlaylistManager(
    IDataAccess dataAccess,
    IOptions<PlaylistManagerOptions> options,
    YouTubeService youTubeService)
{
    private const int _limit = 200;
    private readonly PlaylistManagerOptions _options = options.Value;

    public PlaylistListResponse? GetPlaylists()
    {
        var playlistRequest = youTubeService.Playlists.List("snippet");

        playlistRequest.MaxResults = _limit;
        playlistRequest.ChannelId = _options.ChannelID;

        return playlistRequest.Execute();
    }

    public List<string> GetPlaylistItems(string playlistId, bool id = false)
    {
        List<string> playlistItems = [];

        var playlistItemsRequest = youTubeService.PlaylistItems.List("snippet");

        playlistItemsRequest.MaxResults = _limit;
        playlistItemsRequest.PlaylistId = playlistId;

        PlaylistItemListResponse? playlistItemResponse = new();
        do
        {
            playlistItemsRequest.PageToken = playlistItemResponse.NextPageToken;
            playlistItemResponse = playlistItemsRequest.Execute();

            playlistItems.AddRange(playlistItemResponse.Items.Select(x => id ? x.Snippet.ResourceId.VideoId : x.Snippet.Title));
        }
        while (playlistItemResponse?.NextPageToken != null);

        return playlistItems;
    }

    public void CheckDiff(string playlistTitle, List<string> newTitles, List<string>? downloadPlaylist = null)
    {
        dataAccess.CreateTableIfNotExist(playlistTitle);
        List<string> oldTitles = dataAccess.GetPlaylistItems(playlistTitle);

        var diffTitles = oldTitles.Except(newTitles).ToList();
        if (downloadPlaylist != null)
            diffTitles = [.. diffTitles.Except(downloadPlaylist)];

        diffTitles.RemoveAll(x => x == "Deleted video");
        if (diffTitles.Count != 0)
            dataAccess.InsertDeleted(playlistTitle, diffTitles);

        dataAccess.TruncateTable(playlistTitle);
        dataAccess.InsertPlaylistItems(playlistTitle, newTitles);
    }
}
