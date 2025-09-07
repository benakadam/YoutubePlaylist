using YoutubePlaylist.Interface;

namespace YoutubePlaylist.Helpers;
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now
        => new(
            DateTime.Now.Year,
            DateTime.Now.Month,
            DateTime.Now.Day,
            DateTime.Now.Hour,
            DateTime.Now.Minute,
            DateTime.Now.Second
        );
}
