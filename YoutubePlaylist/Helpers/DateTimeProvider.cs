using YoutubePlaylistManager.Cli.Interface;

namespace YoutubePlaylistManager.Cli.Helpers;
public class DateTimeProvider : IDateTimeProvider
{
    private readonly DateTime _frozenNow;
    public DateTimeProvider()
    {
        // Truncate to seconds
        var now = DateTime.Now;
        _frozenNow = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
    }

    public DateTime Now
        => _frozenNow;
}
