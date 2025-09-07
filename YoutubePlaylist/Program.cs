using Google.Apis.YouTube.v3.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using YoutubePlaylist.Extensions;
using YoutubePlaylist.Helpers;
using YoutubePlaylist.Interface;
using YoutubePlaylist.Manager;
using YoutubePlaylist.Model;

namespace YoutubePlaylist;

public partial class YoutubePlaylist
{
    #region Variables
    private readonly PlaylistManager _playlistManager;
    private readonly DownloadManager _downloadManager;
    private readonly IDataAccess _dataAccess;
    private const string DOWNLOAD = "Download";
    private readonly string _downloadPath;
    private readonly string _playlistBaseUrl;
    private const string SECOND_ATTEMPT = "Egyéb videók";
    private readonly HttpClient _httpClient;
    #endregion

    public YoutubePlaylist()
    {
        _dataAccess = DIContainer.Resolve<IDataAccess>();
        _playlistManager = new(_dataAccess);
        _downloadPath = Helper.GetConfigValue("DownloadPath");
        _downloadManager = new(_downloadPath);
        _httpClient = new();
        _playlistBaseUrl = Helper.GetConfigValue("PlaylistBaseUrl");
    }

    [STAThread]
    static async Task Main()
    {
        if (!Helper.IsInternetAvailable())
        {
            Console.WriteLine("Nincs internet kapcsolat!");
            return;
        }

        await new YoutubePlaylist().StartProcess();
    }
   
    public async Task StartProcess()
    {
        try
        {
            var playlistsResponse = _playlistManager.GetPlaylists();
            if (playlistsResponse is null)
            {
                Console.WriteLine("Nem található lejátszási lista");
                return;
            }

            Playlist downloadPlaylist = playlistsResponse.Items.First(x => x.Snippet.Title == DOWNLOAD);
            List<string> downloadVideos = _playlistManager.GetPlaylistItems(downloadPlaylist.Id);
            List<string> downloadIDs = _playlistManager.GetPlaylistItems(downloadPlaylist.Id, true);

            List<Playlist> playlists = [.. playlistsResponse.Items.Where(x => x.Snippet.Title != DOWNLOAD)];

            CheckForMissingVideosFirstAttempt(playlists, downloadVideos);
            var affectedPlaylistIds = await CheckForMissingVideosSecondAttempt(playlists);

            List<Deleted> deletedVideos = _dataAccess.GetLatestDeleted();
            var index = deletedVideos.FindIndex(d => d.Playlist == SECOND_ATTEMPT);
            WriteOutResult(deletedVideos.Take(index));
            Console.WriteLine($"{SECOND_ATTEMPT}:");
            WriteOutResult(deletedVideos.Skip(index + 1));

            OpenAffectedPlaylistsInBrowser(affectedPlaylistIds);

            if (downloadIDs.Count == 0) return;          
            await DownloadVideos(downloadIDs, downloadVideos);
        }
        catch (Exception ex)
        {           
            Console.WriteLine("Error: " + ex.Message);  
        }

        Console.WriteLine("\nKész vagyunk Mester!");
        Console.ReadKey();
    }

    private void CheckForMissingVideosFirstAttempt(List<Playlist> playlists, List<string> downloadVideos)
    {
        Console.WriteLine("Könyvtár karbantartása");
        foreach (var playlist in playlists)
        {
            var videos = _playlistManager.GetPlaylistItems(playlist.Id);
            if (videos.Count == 0) continue;
            
            _playlistManager.CheckDiff(playlist.Snippet.Title, videos, downloadVideos);
        }
    }

    private async Task<List<string>> CheckForMissingVideosSecondAttempt(List<Playlist> playlists)
    {
        _dataAccess.InsertDeleted(SECOND_ATTEMPT, [""]);
        List<string> affectedPlaylistIds = [];

        foreach (var playlist in playlists)
        {
            string url = _playlistBaseUrl + playlist.Id;

            string html = await _httpClient.GetStringAsync(url);

            var match = Regexes.YtInitialData().Match(html);
            if (!match.Success) continue;

            using var jsonDoc = JsonDocument.Parse(match.Groups[1].Value);
            var root = jsonDoc.RootElement;

            bool hasHiddenVideos = root
                .Descendants()
                .Any(e => e.ValueKind == JsonValueKind.String &&
                          e.GetString() == "A rendelkezésre nem álló videók el vannak rejtve");

            if (hasHiddenVideos) affectedPlaylistIds.Add(playlist.Id); 

            MatchCollection matches = Regexes.TitleRuns().Matches(html);
            var matchList = matches.Take(matches.Count - 7);

            List<string> videos = [.. matchList.Select(x => Regex.Unescape(x.Groups[1].Value))];
            List<string> allVideos = _dataAccess.GetPlaylistItems(playlist.Snippet.Title);

            List<string> missings = [.. allVideos.Take(100).Except(videos)];

            if (missings.Count == 0) continue;

            _dataAccess.InsertDeleted(playlist.Snippet.Title, missings);
        }

        return affectedPlaylistIds;
    }

    private static void WriteOutResult(IEnumerable<Deleted> deletedVideos)
    {
        foreach (var group in deletedVideos.GroupBy(d => d.Playlist))
        {
            Console.WriteLine(group.Key);
            foreach (var item in group)
            {
                Console.WriteLine(item.Title);
            }
            Console.WriteLine();
        }
    }

    private void OpenAffectedPlaylistsInBrowser(List<string> affectedPlaylistIds)
        => affectedPlaylistIds.ForEach(id =>
                Process.Start(new ProcessStartInfo
                {
                    FileName = _playlistBaseUrl + id,
                    UseShellExecute = true
                }));


    private async Task DownloadVideos(List<string> downloadIDs, List<string> titles)
    {
        Console.WriteLine("Letöltés");
        Directory.CreateDirectory(_downloadPath);
        string baseUrl = Helper.GetConfigValue("VideoBaseUrl");

        var downloadTasks = downloadIDs.Select(id => _downloadManager.DownloadWebmAudioAsync(baseUrl + id)).ToList();

        var progressTask = Task.Run(() => ConsoleManager.ShowProgressBarWhileTasksRunning(downloadTasks));

        await Task.WhenAll(downloadTasks);
        await progressTask;

        Console.WriteLine($"\nA letöltés elkészült Mester!");
        CheckForUnsuccesfulDownloads(titles, RenameFiles());
    }

    private IEnumerable<string> RenameFiles()
    {
        var files = Directory.GetFiles(_downloadPath)
            .Select(x => Path.GetFileNameWithoutExtension(x));

        foreach (string file in files)
        {
            string newFileName = ConsoleManager.ReadInputWithDefault(file);
            if (newFileName == file) continue;

            string destinationPath = Path.Combine(_downloadPath, newFileName + ".mp3");
            if (File.Exists(destinationPath)) continue;
            
            File.Move(Path.Combine(_downloadPath, file + ".mp3"), destinationPath);
            _dataAccess.InsertPlaylistItem("ALLSONGS", newFileName);           
        }

        return files;
    }

    private static void CheckForUnsuccesfulDownloads(List<string> titles, IEnumerable<string> files)
    {
        files = files.Select(x => Regexes.FileTitle().Replace(x, ""));

        List<string> unsuccesfulDownloads = [.. titles.Where(title => !files.Contains(Regexes.FileTitle().Replace(title, "")))];

        if (unsuccesfulDownloads.Count > 0)
        {
            Console.WriteLine("\nA következő számok letöltése nem sikerült:");
            unsuccesfulDownloads.ForEach(x =>  Console.WriteLine(x));
        }
    }
}
