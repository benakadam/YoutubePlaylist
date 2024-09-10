using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using YoutubePlaylist.Manager;
using YoutubePlaylist.Helpers;
using YoutubePlaylist.Interface;
using System.Globalization;
using System.Net;

namespace YoutubePlaylist;

public class YoutubePlaylist
{
    #region Variables
    private readonly PlaylistManager _playlistManager;
    private readonly DownloadManager _downloadManager;
    private readonly IDataAccess _dataAccess;
    private const string DOWNLOAD = "Download";
    private readonly string _downloadPath;
    private readonly Regex _titleRegex;
    #endregion

    public YoutubePlaylist()
    {
        _dataAccess = Helper.ResolveInterface<IDataAccess>();
        _playlistManager = new(_dataAccess);
        _downloadPath = Helper.GetConfigValue("DownloadPath");
        _downloadManager = new(_downloadPath);
        _titleRegex = new Regex(@"[^a-zA-Z0-9 ]");
    }

    [STAThread]
    static async Task Main(string[] args)
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

            List<Playlist> playlists = playlistsResponse.Items.Where(x => x.Snippet.Title != DOWNLOAD).ToList();

            CheckForMissingVideosFirstAttempt(playlists, downloadVideos);
            await CheckForMissingVideosSecondAttempt(playlists);

            foreach (var group in _dataAccess.GetDeleted().GroupBy(d => d.Playlist))
            {
                Console.WriteLine(group.Key);
                foreach (var item in group)
                {
                    Console.WriteLine(item.Title);
                }
                Console.WriteLine();
            }

            if (downloadIDs.Any())
            {
                HashSet<string> titles = new HashSet<string>(downloadVideos.Select(x => _titleRegex.Replace(x, "")));
                DownloadVideos(downloadIDs, titles);
                return;
            }
            if (!Directory.EnumerateFileSystemEntries(_downloadPath).Any())
                Directory.Delete(_downloadPath);
        }
        catch (AggregateException ex)
        {
            foreach (var e in ex.InnerExceptions)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        Console.WriteLine("\nKész vagyok Mester!");
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

    private async Task CheckForMissingVideosSecondAttempt(List<Playlist> playlists)
    {
        string baseUrl = Helper.GetConfigValue("PlaylistBaseUrl");
        _dataAccess.InsertDeleted("Egyéb videók", new List<string>() { "" });

        foreach (var playlist in playlists)
        {
            string url = baseUrl + playlist.Id;

            using (HttpClient httpClient = new HttpClient())
            {
                string html = await httpClient.GetStringAsync(url);

                MatchCollection matches = Regex.Matches(html, "\"title\":{\"runs\":\\[{\"text\":\"(.*?)\"\\}");

                List<string> videos = matches.Take(matches.Count - 8)
                    .Select(x => Regex.Unescape(x.Groups[1].Value)).ToList();
                List<string> allVideos = _dataAccess.GetPlaylistItems(playlist.Snippet.Title);

                List<string> missings = allVideos.Take(100).Except(videos).ToList();

                if (missings.Count == 0) continue;

                _dataAccess.InsertDeleted(playlist.Snippet.Title, missings);
            }
        }
    }

    private void DownloadVideos(List<string> downloadIDs, HashSet<string> titles)
    {
        Console.WriteLine("Letöltés");
        string baseUrl = Helper.GetConfigValue("VideoBaseUrl");
        downloadIDs.ForEach(id => _downloadManager.DownloadWebmAudio(baseUrl + id));

        while (true)
        {
            if (IsDownloadFinished(titles))
            {
                Console.WriteLine($"\nA letöltés elkészült Mester!");
                break;
            }
            
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, Console.CursorTop); Thread.Sleep(300);
            for (int i = 0; i < 6; i++)
            {
                Console.Write(":");
                Thread.Sleep(100);
            }
            Thread.Sleep(600);
        }
        RenameFiles(titles);
    }

    private bool IsDownloadFinished(HashSet<string> titles)
    {
        bool isFinished = false;

        foreach (var filePath in Directory.EnumerateFiles(_downloadPath))
        {
            isFinished = true;
            if (!Path.GetExtension(filePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!titles.Contains(_titleRegex.Replace(Path.GetFileNameWithoutExtension(filePath), "")))
            {
                return false;
            }
        }
        return isFinished;
    }

    private void RenameFiles(HashSet<string> titles)
    {
        var files = Directory.GetFiles(_downloadPath)
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Where(y => titles.Contains(_titleRegex.Replace(y ,"")))
            .ToList();

        foreach (string file in files)
        {
            string newFileName = DirectoryManager.ReadInputWithDefault(file);
            if (newFileName != file)
                File.Move(Path.Combine(_downloadPath, file + ".mp3"), Path.Combine(_downloadPath, newFileName + ".mp3"));

            _dataAccess.InsertPlaylistItem("ALLSONGS", newFileName);
        }
    }
}
