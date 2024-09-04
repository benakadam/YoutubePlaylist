using System.Data;

namespace YoutubePlaylist.Helpers;
public static class DbHelper
{
    public static T ExecuteWithConnection<T>(Func<IDbConnection, T> query)
    {
        using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(Helper.GetConnectionString("DBConnection")))
        {
            return query(connection);
        }
    }

    public static void ExecuteWithConnection(Action<IDbConnection> query)
    {
        using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(Helper.GetConnectionString("DBConnection")))
        {
            query(connection);
        }
    }
}
