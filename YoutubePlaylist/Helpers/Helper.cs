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

    public static T ResolveInterface<T>()
    {
        string typeName = GetConfigValue(typeof(T).Name);

        if (string.IsNullOrEmpty(typeName))
            throw new InvalidOperationException($"{typeof(T).Name} is not configured in App.config.");

        Type type = Type.GetType(typeName);

        if (type is null)
            throw new InvalidOperationException($"Could not find type: {typeName}");

        if (!typeof(T).IsAssignableFrom(type))
            throw new InvalidOperationException($"{typeName} does not implement {typeof(T).Name}");

        return (T)Activator.CreateInstance(type);
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
