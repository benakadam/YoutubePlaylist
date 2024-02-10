using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace YoutubePlaylist;
public class PlaylistManager
{
    private const string _apiKey = "YOUR_API_KEY";
    private const string _channelID = "YOUR_CHANNEL_ID";
    private const string _appName = "YouTubePlaylist";
    private const int _limit = 200;
    private readonly string _savePath;
    private readonly YouTubeService _youtubeService;
    private readonly string _resultPath;
    public PlaylistManager()
    {
        _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YoutubePlaylist");
        Directory.CreateDirectory(_savePath);
        //var clientSecrets = new ClientSecrets
        //{
        //    ClientId = "",
        //    ClientSecret = ""
        //};

        //// OAuth 2.0 hitelesítés
        //var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
        //    clientSecrets,
        //    new[] { YouTubeService.Scope.Youtube },
        //    "user",
        //    CancellationToken.None).Result;

        _youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = _apiKey,
            //HttpClientInitializer = credential,
            ApplicationName = _appName
        });

        _resultPath = Path.Combine(_savePath, "RESULT" + ".txt");
        if (!File.Exists(_resultPath))
            File.Create(_resultPath).Close();
        else
            File.WriteAllText(_resultPath, string.Empty);
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
        string path = Path.Combine(_savePath, playlistTitle + ".txt");
        if (!File.Exists(path))
            File.Create(path).Close();

        List<string> oldTitles = File.ReadAllLines(path).ToList();

        var diffTitles = oldTitles.Except(newTitles).ToList();
        if (downloadPlaylist != null)
            diffTitles = diffTitles.Except(downloadPlaylist).ToList();

        diffTitles.RemoveAll(x => x == "Deleted video");
        if (diffTitles.Any())
        {
            string deletedLine = $"\nTörölt elemek a {playlistTitle} lejátszási listából:";
            Console.WriteLine(deletedLine);
            diffTitles.ForEach(x => Console.WriteLine(x));
            File.AppendAllLines(_resultPath, new[] { deletedLine, string.Join("\n", diffTitles) });
        }
        File.WriteAllLines(path, newTitles);
    }

    //oauth2 kell hozza
    //public void DeleteVideo(string videoID)
    //{
    //    var request = _youtubeService.PlaylistItems.Delete(videoID);
    //    request.Execute();
    //}

    private string RemoveTextInBrackets(string input)
    {
        string pattern = @"\[[^\]]*\]|\([^)]*\)";

        string result = Regex.Replace(input, pattern, "");

        return result.Trim();
    }
}
