using NReco.VideoConverter;
using System.Diagnostics;

namespace YoutubePlaylist.Manager;
public class DownloadManager
{
    private readonly string _downloadPath;
    private bool _isUpdated = false;

    private readonly FFMpegConverter _converter = new FFMpegConverter();

    public DownloadManager(string downloadPath) => _downloadPath = downloadPath;

    public async Task DownloadWebmAudioAsync(string url)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
        string exePath = Path.Combine(projectRoot, "Thirdparty", "yt-dlp.exe");

        if (!_isUpdated)
        {
            var updateProcess = InitProcess(exePath, "-U"); //Ez frissíti a letöltő programot ha nem működne
            updateProcess.Start();
            updateProcess.WaitForExit();
            _isUpdated = true;
        }   

        var process = InitProcess(exePath,
            $" -f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 {url} -o {_downloadPath}\\%(title)s.%(ext)s");

        process.EnableRaisingEvents = true;

        var tcs = new TaskCompletionSource<bool>();
        process.Exited += (sender, args) =>
        {
            tcs.SetResult(true);
            process.Dispose();
        };

        process.Start();
        await tcs.Task;
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
