namespace YoutubePlaylistManager.Cli.Model;
public class Deleted
{
    public required string Playlist { get; set; }
    public required string Title { get; set; }
    public required DateTime DeletionDate { get; set; }
}
