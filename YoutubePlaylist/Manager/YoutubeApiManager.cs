﻿using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Options;
using YoutubePlaylistManager.Cli.Options;

namespace YoutubePlaylistManager.Cli.Manager;
public class YoutubeApiManager(
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
}
