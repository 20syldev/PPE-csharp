using Npgsql;

namespace PPE.Modele
{
    public class Connect
    {
        public Connect() {}

        public string? Server { get; set; }
        public string? DatabaseName { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public int Port { get; set; } = 5432;

        public NpgsqlConnection? Connection { get; set; }

        private static Connect? _instance = null;
        public static Connect Instance()
        {
            _instance ??= new Connect();
            return _instance;
        }

        public bool IsConnect()
        {
            if (Connection == null)
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
                Connection = new NpgsqlConnection(builder.ConnectionString);
                Connection.Open();
            }

            return true;
        }

        public void Close()
        {
            Connection?.Close();
        }
    }
}
