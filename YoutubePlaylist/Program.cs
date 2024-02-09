namespace YoutubePlaylist;

using Google.Apis.YouTube.v3.Data;
using System;
using System.IO;

public class YoutubePlaylist
{
    private static readonly PlaylistManager _playlistController = new();
    private static readonly string DOWNLOAD = "Download";
    private static readonly string _downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Youtube");


    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Könyvtár karbantartása");
            DownloadManager.SetEnvironment();
            var playlists = _playlistController.GetPlaylists();
            if (playlists is null)
            {
                Console.WriteLine("Nem található lejátszási lista");
                return;
            }

            Playlist downloadPlaylist = playlists.Items.First(x => x.Snippet.Title == DOWNLOAD);
            List<string> downloadVideos = _playlistController.GetPlaylistItems(downloadPlaylist.Id);
            List<string> downloadIDs = _playlistController.GetPlaylistItems(downloadPlaylist.Id, true);

            foreach (var playlist in playlists.Items.Where(x => x.Snippet.Title != DOWNLOAD))
            {
                var videos = _playlistController.GetPlaylistItems(playlist.Id);
                if (!videos.Any())
                {
                    Console.WriteLine($"Nem található videó a(z) {playlist.Snippet.Title} lejátszási listán");
                    continue;
                }
                _playlistController.CheckDiff(playlist.Snippet.Title, videos, downloadVideos);
            }
            if (downloadIDs.Any())
            {
                DownloadVideos(downloadIDs, downloadVideos);
                RenameFiles();
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

        Console.WriteLine("Készen állok Mester!");
        Console.ReadKey();
    }


    private static void DownloadVideos(List<string> downloadIDs, List<string> titles)
    {
        Console.WriteLine("Letöltés");
        downloadIDs.ForEach(id => DownloadManager.DownloadWebmAudio($"https://www.youtube.com/watch?v={id}"));

        List<string> files;

        while (true)
        {
            files = Directory.GetFiles(_downloadPath).Select(x => Path.GetFileName(x)).ToList();

            if (files.All(file => Path.GetExtension(file).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                && titles.Select(x => $"{x}.mp3").All(title => files.Contains(title)))
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
    }

    private static void RenameFiles()
    {
        string[] files = Directory.GetFiles(_downloadPath);
        foreach (string file in files)
        {
            string newFileName = DirectoryManager.ReadInputWithDefault(Path.GetFileNameWithoutExtension(file));
            if (newFileName != file)
                File.Move(Path.Combine(_downloadPath, file), Path.Combine(_downloadPath, newFileName + ".mp3"));
        }
    }
}
