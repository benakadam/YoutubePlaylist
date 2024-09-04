using NReco.VideoConverter;
using System.Diagnostics;

namespace YoutubePlaylist.Manager;
public class DownloadManager
{
    private readonly string _downloadPath;

    private readonly FFMpegConverter _converter = new FFMpegConverter();

    public DownloadManager(string downloadPath)
    {
        _downloadPath = downloadPath;
        Directory.CreateDirectory(downloadPath);
    }


    public Process DownloadWebmAudio(string url)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));

        string exePath = Path.Combine(projectRoot, "Thirdparty", "yt-dlp.exe");

        return InitProcess(exePath,
            $@" -f bestaudio  --extract-audio --audio-format mp3 --audio-quality 0 {url} -o {_downloadPath}\%(title)s.%(ext)s");
    }


    private Process InitProcess(string fileName, string args)
    {
        Process process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = args;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }
}
