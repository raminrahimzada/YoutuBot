using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace YoutuBot
{
    public static class Sql
    {
        private static SqlConnection _connection;

        private static string  connectionString =
            "Data Source=MATRIX\\SERVER17;Initial Catalog=Youtube;User Id=sa;Password=badimcandolmasi;Connect Timeout=100000;";
        
        private static void Ensure()
        {
            if (_connection == null)
                _connection =
                    new SqlConnection(connectionString);
            if(_connection.State!=ConnectionState.Open)_connection.Open();
        }

        public static T[] Execute<T>(string query)
        {
            lock (Sync)
            {
                Ensure();
                return _connection.Query<T>(query).ToArray();
            }
        }

        private static readonly object Sync=new object();
        public static void Execute(string query)
        {
            lock (Sync)
            {
                Ensure();
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}