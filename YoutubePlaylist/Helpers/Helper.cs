using System.Configuration;
using System.Net.NetworkInformation;
namespace YoutubePlaylist.Helpers;

public static class Helper
{
    public static string GetConnectionString(string name)
        => ConfigurationManager.ConnectionStrings[name].ConnectionString; 
    
    public static string GetConfigValue(string name)
        => ConfigurationManager.AppSettings[name] ?? "";

    public static string SanitizeTableName(string tableName)
    {
        if (char.IsDigit(tableName[0]))
            tableName = "_" + tableName;

        return tableName.Replace(" ", "_");
    }

    public static bool IsInternetAvailable()
    {
        try
        {
            return new Ping()
                .Send("8.8.8.8", 3000)
                .Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
