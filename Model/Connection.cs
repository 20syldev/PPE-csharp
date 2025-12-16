using Npgsql;

namespace PPE.Model
{
    public class Connection
    {
        public Connection() {}

        public string? Server { get; set; }
        public string? DatabaseName { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public int Port { get; set; } = 5432;

        public NpgsqlConnection? DbConnection { get; set; }

        private static Connection? _instance = null;
        public static Connection Instance()
        {
            _instance ??= new Connection();
            return _instance;
        }

        public bool IsConnect()
        {
            if (DbConnection == null)
            {
                if (string.IsNullOrEmpty(DatabaseName)) return false;

                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = Server,
                    Port = Port,
                    Database = DatabaseName,
                    Username = UserName,
                    Password = Password
                };
                DbConnection = new NpgsqlConnection(builder.ConnectionString);
                DbConnection.Open();
            }

            return true;
        }

        public void Close()
        {
            DbConnection?.Close();
        }
    }
}
