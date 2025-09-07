using System.Data;
using Dapper;
using YoutubePlaylist.Helpers;
using YoutubePlaylist.Interface;
using YoutubePlaylist.Model;

namespace YoutubePlaylist.DataAccess;

public class DataAccess : IDataAccess
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public DataAccess(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public List<string> GetPlaylistItems(string playlistName)
    {
        playlistName = Helper.SanitizeTableName(playlistName);

        return DbHelper.ExecuteWithConnection(connection =>
        {
            return connection.Query<string>($"SELECT Name FROM {playlistName}").ToList();
        });
    }

    public bool CreateTableIfNotExist(string tableName)
    {
        tableName = Helper.SanitizeTableName(tableName);

        if (DoesTableExist(tableName)) return false;
        

        DbHelper.ExecuteWithConnection(connection =>
        {
            connection.Execute($"CREATE TABLE {tableName} (Name NVARCHAR(255))");
        });
        return true;
    }

    public void InsertPlaylistItem(string playlist, string playlistItem)
    {
        playlist = Helper.SanitizeTableName(playlist);

        DbHelper.ExecuteWithConnection(connection =>
        {
            connection.Execute($"INSERT INTO {playlist}(Name) VALUES(@PlaylistItem)", new { PlaylistItem = playlistItem});
        });       
    }

    public void InsertPlaylistItems(string playlist, List<string> playlistItems)
    {
        playlist = Helper.SanitizeTableName(playlist);

        DbHelper.ExecuteWithConnection(connection =>
        {
            var parameters = playlistItems.Select(item => new { PlaylistItem = item }).ToArray();

            connection.Execute($"INSERT INTO {playlist}(Name) VALUES(@PlaylistItem)", parameters);
        });
    }

    public void InsertDeleted(string playlist, List<string> playlistItems)
    {
        DbHelper.ExecuteWithConnection(connection =>
        {
            var parameters = playlistItems.Select(item => new
            {
                Playlist = playlist,
                PlaylistItem = item,
                DeletedAt = _dateTimeProvider.Now,
            }).ToArray();

            connection.Execute($"INSERT INTO DELETED(Playlist, Title, DeletedAt) VALUES(@Playlist, @PlaylistItem, @DeletedAt)", parameters);
        });
    }

    public List<Deleted> GetLatestDeleted()
    {
        return DbHelper.ExecuteWithConnection(connection =>
        {
            return connection.Query<Deleted>($"SELECT * FROM DELETED WHERE DeletedAt = @DeletedAt",
                new { DeletedAt = _dateTimeProvider.Now }).ToList();
        });
    }

    public void TruncateTable(string tableName)
    {
        tableName = Helper.SanitizeTableName(tableName);

        DbHelper.ExecuteWithConnection(connection =>
        {
            connection.Execute($"TRUNCATE TABLE {tableName}");
        });
    }

    private bool DoesTableExist(string tableName)
    {
        return DbHelper.ExecuteWithConnection(connection =>
        {
            string query = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
            return connection.QuerySingle<int>(query, new { TableName = tableName }) > 0;
        });
    }
}