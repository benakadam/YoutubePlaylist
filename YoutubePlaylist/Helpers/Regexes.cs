using System.Text.RegularExpressions;

namespace YoutubePlaylist.Helpers;
public static partial class Regexes
{
    [GeneratedRegex(@"[^\p{L}\p{N} ]", RegexOptions.Compiled)]
    public static partial Regex FileTitle();

    [GeneratedRegex(@"var ytInitialData = ({.*?});</script>", RegexOptions.Compiled)]
    public static partial Regex YtInitialData();

    [GeneratedRegex("\"title\":\\{\"runs\":\\[\\{\"text\":\"(.*?)\"\\}", RegexOptions.Compiled)]
    public static partial Regex TitleRuns();
}
