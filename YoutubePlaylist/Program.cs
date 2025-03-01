using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using YoutubePlaylist.Manager;
using YoutubePlaylist.Helpers;
using YoutubePlaylist.Interface;
using YoutubePlaylist.Model;
using System.Diagnostics;

namespace YoutubePlaylist;

public class YoutubePlaylist
{
    #region Variables
    private readonly PlaylistManager _playlistManager;
    private readonly DownloadManager _downloadManager;
    private readonly IDataAccess _dataAccess;
    private const string DOWNLOAD = "Download";
    private readonly string _downloadPath;
    private readonly string _playlistBaseUrl;
    private const string SECOND_ATTEMPT = "Egyéb videók";
    private List<Playlist> _playlists;
    #endregion

    public YoutubePlaylist()
    {
        _dataAccess = Helper.ResolveInterface<IDataAccess>();
        _playlistManager = new(_dataAccess);
        _downloadPath = Helper.GetConfigValue("DownloadPath");
        _downloadManager = new(_downloadPath);

        _playlistBaseUrl = Helper.GetConfigValue("PlaylistBaseUrl");
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

            _playlists = playlistsResponse.Items.Where(x => x.Snippet.Title != DOWNLOAD).ToList();

            CheckForMissingVideosFirstAttempt(downloadVideos);
            await CheckForMissingVideosSecondAttempt();

            List<Deleted> deletedVideos = _dataAccess.GetDeleted();
            var index = deletedVideos.FindIndex(d => d.Playlist == SECOND_ATTEMPT);
            WriteOutResult(deletedVideos.Take(index));
            Console.WriteLine($"{SECOND_ATTEMPT}:");
            WriteOutResult(deletedVideos.Skip(index + 1));

            OpenAffectedPlaylistsInBrowser(deletedVideos.Take(index));

            if (!downloadIDs.Any()) return;          
            await DownloadVideos(downloadIDs, downloadVideos);
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

    private void CheckForMissingVideosFirstAttempt(List<string> downloadVideos)
    {
        Console.WriteLine("Könyvtár karbantartása");
        foreach (var playlist in _playlists)
        {
            var videos = _playlistManager.GetPlaylistItems(playlist.Id);
            if (videos.Count == 0) continue;
            
            _playlistManager.CheckDiff(playlist.Snippet.Title, videos, downloadVideos);
        }
    }

    private async Task CheckForMissingVideosSecondAttempt()
    {
        _dataAccess.InsertDeleted(SECOND_ATTEMPT, new List<string>() { "" });

        foreach (var playlist in _playlists)
        {
            string url = _playlistBaseUrl + playlist.Id;
            
            using (HttpClient httpClient = new HttpClient())
            {
                string html = await httpClient.GetStringAsync(url);

                MatchCollection matches = Regex.Matches(html, "\"title\":{\"runs\":\\[{\"text\":\"(.*?)\"\\}");
                var matchList = matches.Take(matches.Count - 7);

                List<string> videos = matchList
                    .Select(x => Regex.Unescape(x.Groups[1].Value)).ToList();
                List<string> allVideos = _dataAccess.GetPlaylistItems(playlist.Snippet.Title);

                List<string> missings = allVideos.Take(100).Except(videos).ToList();

                if (missings.Count == 0) continue;

                _dataAccess.InsertDeleted(playlist.Snippet.Title, missings);
            }
        }
    }

    private void WriteOutResult(IEnumerable<Deleted> deletedVideos)
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

    private void OpenAffectedPlaylistsInBrowser(IEnumerable<Deleted> deletedVideos)
    {
        var affectedListIds = _playlists
            .Where(x => deletedVideos.Any(y => y.Playlist == x.Snippet.Title))
            .Select(x => x.Id)
            .ToList();

        affectedListIds.ForEach(id =>
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _playlistBaseUrl + id,
                        UseShellExecute = true
                    }));
    }


    private async Task DownloadVideos(List<string> downloadIDs, List<string> titles)
    {
        Console.WriteLine("Letöltés");
        Directory.CreateDirectory(_downloadPath);
        string baseUrl = Helper.GetConfigValue("VideoBaseUrl");

        var downloadTasks = downloadIDs.Select(id => _downloadManager.DownloadWebmAudioAsync(baseUrl + id)).ToList();

        var progressTask = Task.Run(async () =>
        {
            while (!downloadTasks.All(t => t.IsCompleted))
            {
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
        });

        await Task.WhenAll(downloadTasks);
        await progressTask;

        Console.WriteLine($"\nA letöltés elkészült Mester!");
        var files = RenameFiles(titles);
        CheckForUnsuccesfulDownloads(titles, files);
    }


    private IEnumerable<string> RenameFiles(List<string> titles)
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

    private void CheckForUnsuccesfulDownloads(List<string> titles, IEnumerable<string> files)
    {
        var titleRegex = new Regex(@"[^a-zA-Z0-9 ]");
        files = files.Select(x => titleRegex.Replace(x, ""));

        List<string> unsuccesfulDownloads = titles.Where(title => !files.Contains(titleRegex.Replace(title, ""))).ToList();

        if (unsuccesfulDownloads.Count > 0)
        {
            Console.WriteLine("\nA következő számok letöltése nem sikerült:");
            unsuccesfulDownloads.ForEach(x =>  Console.WriteLine(x));
        }
    }
}
