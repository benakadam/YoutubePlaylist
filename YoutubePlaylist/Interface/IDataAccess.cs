﻿using YoutubePlaylist.Model;

namespace YoutubePlaylist.Interface;
public interface IDataAccess
{
    public List<string> GetPlaylistItems(string playlistName);

    public bool CreateTableIfNotExist(string tableName);

    public void InsertPlaylistItem(string playlist, string playlistItem);

    public void InsertPlaylistItems(string playlist, List<string> playlistItems);

    public void InsertDeleted(string playlist, List<string> playlistItems);

    public List<Deleted> GetDeleted();

    public void TruncateTable(string tableName);
}
