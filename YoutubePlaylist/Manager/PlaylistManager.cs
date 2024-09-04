using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using YoutubePlaylist.Interface;
using YoutubePlaylist.Helpers;

namespace YoutubePlaylist.Manager;
public class PlaylistManager
{
    private readonly string _apiKey;
    private readonly string _channelID;
    private const int _limit = 200;
    private readonly YouTubeService _youtubeService;
    private readonly IDataAccess _dataAccess;
    public PlaylistManager(IDataAccess dataAccess)
    {
        #region  OAuth 2.0 hitelesítés
        //var clientSecrets = new ClientSecrets
        //{
        //    ClientId = "",
        //    ClientSecret = ""
        //};

        //var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
        //    clientSecrets,
        //    new[] { YouTubeService.Scope.Youtube },
        //    "user",
        //    CancellationToken.None).Result;
        #endregion

        _apiKey = Helper.GetConfigValue("ApiKey");
        _channelID = Helper.GetConfigValue("ChannelID");
        _dataAccess = dataAccess;
        _youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = _apiKey,
            //HttpClientInitializer = credential,
            ApplicationName = "YouTubePlaylist"
        });
    }

    public PlaylistListResponse? GetPlaylists()
    {
        var playlistRequest = _youtubeService.Playlists.List("snippet");

        playlistRequest.MaxResults = _limit;
        playlistRequest.ChannelId = _channelID;

        return playlistRequest.Execute();
    }

    public List<string> GetPlaylistItems(string playlistId, bool id = false)
    {
        List<string> playlistItems = new List<string>();

        var playlistItemsRequest = _youtubeService.PlaylistItems.List("snippet");

        playlistItemsRequest.MaxResults = _limit;
        playlistItemsRequest.PlaylistId = playlistId;

        PlaylistItemListResponse? playlistItemResponse = new();
        do
        {
            playlistItemsRequest.PageToken = playlistItemResponse.NextPageToken;
            playlistItemResponse = playlistItemsRequest.Execute();

            playlistItemResponse.Items.ToList().ForEach(
                    x => playlistItems.Add(
                        id ? x.Snippet.ResourceId.VideoId : x.Snippet.Title));
        }
        while (playlistItemResponse?.NextPageToken != null);

        return playlistItems;
    }

    public void CheckDiff(string playlistTitle, List<string> newTitles, List<string>? downloadPlaylist = null)
    {
        _dataAccess.CreateTableIfNotExist(playlistTitle);
        List<string> oldTitles = _dataAccess.GetPlaylistItems(playlistTitle);

        var diffTitles = oldTitles.Except(newTitles).ToList();
        if (downloadPlaylist != null)
            diffTitles = diffTitles.Except(downloadPlaylist).ToList();

        diffTitles.RemoveAll(x => x == "Deleted video");
        if (diffTitles.Any())
            _dataAccess.InsertDeleted(playlistTitle, diffTitles);

        _dataAccess.TruncateTable(playlistTitle);
        _dataAccess.InsertPlaylistItems(playlistTitle, newTitles);
    }

    private string RemoveTextInBrackets(string input)
    {
        string pattern = @"\[[^\]]*\]|\([^)]*\)";

        string result = Regex.Replace(input, pattern, "");

        return result.Trim();
    }
}
