using System.Diagnostics;

namespace YoutubePlaylist.Manager;
public class DownloadManager(string downloadPath)
{
    private bool _isUpdated = false;

    public async Task DownloadWebmAudioAsync(string url)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
        string exePath = Path.Combine(projectRoot, "Thirdparty", "yt-dlp.exe");

        if (!_isUpdated)
        {
            var updateProcess = InitProcess(exePath, "-U");
            updateProcess.Start();
            updateProcess.WaitForExit();
            _isUpdated = true;
        }   

        var process = InitProcess(exePath,
            $" -f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 {url} -o {downloadPath}\\%(title)s.%(ext)s");

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


    private static Process InitProcess(string fileName, string args)
    {
        Process process = new();
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
