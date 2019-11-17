using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace YoutuBot
{
    public class Sql
    {
        private static SqlConnection _connection;

        public static string  connectionString =
            "Data Source=MATRIX\\SERVER17;Initial Catalog=Youtube;User Id=sa;Password=badimcandolmasi;Connect Timeout=50000;";
        
        private static void Ensure()
        {
            if (_connection == null)
                _connection =
                    new SqlConnection(connectionString);
            if(_connection.State!=ConnectionState.Open)_connection.Open();
        }

        public static T[] Execute<T>(string query)
        {
            lock (sync)
            {
                Ensure();
                return _connection.Query<T>(query).ToArray();
            }
        }
        public static object sync=new object();
        public static void Execute(string query)
        {
            lock (sync)
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