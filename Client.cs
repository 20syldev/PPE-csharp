using Npgsql;

namespace PPE
{
    public class Client
    {
        public Guid? Id { get; set; }
        public string? Nom { get; set; }
        public string? Adresse { get; set; }
        public string? Ville { get; set; }
        public string? Code { get; set; }

        public Client() { }

        public Client(string nom, string adresse, string ville, string code)
        {
            Nom = nom;
            Adresse = adresse;
            Ville = ville;
            Code = code;
        }

        public bool Enregistrer()
        {
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                Id = Guid.NewGuid();

                using var cmd = new NpgsqlCommand("CALL client_add(@id, @nom, @adresse, @ville, @code)", db.Connection);
                cmd.Parameters.AddWithValue("@id", Id.Value);
                cmd.Parameters.AddWithValue("@nom", Nom ?? "");
                cmd.Parameters.AddWithValue("@adresse", Adresse ?? "");
                cmd.Parameters.AddWithValue("@ville", Ville ?? "");
                cmd.Parameters.AddWithValue("@code", Code ?? "");

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de l'enregistrement : " + ex.Message);
                return false;
            }
        }

        public static List<Client> ListerTous()
        {
            var clients = new List<Client>();
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return clients;

                using var cmd = new NpgsqlCommand("SELECT * FROM client_list()", db.Connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    clients.Add(new Client
                    {
                        Id = reader.GetGuid(0),
                        Nom = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Adresse = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Ville = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Code = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la lecture : " + ex.Message);
            }

            return clients;
        }

        public bool Modifier()
        {
            if (Id == null) return false;
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL client_update(@id, @nom, @adresse, @ville, @code)", db.Connection);
                cmd.Parameters.AddWithValue("@id", Id.Value);
                cmd.Parameters.AddWithValue("@nom", Nom ?? "");
                cmd.Parameters.AddWithValue("@adresse", Adresse ?? "");
                cmd.Parameters.AddWithValue("@ville", Ville ?? "");
                cmd.Parameters.AddWithValue("@code", Code ?? "");

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la modification : " + ex.Message);
                return false;
            }
        }

        public static bool Supprimer(Guid id)
        {
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL client_delete(@id)", db.Connection);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la suppression : " + ex.Message);
                return false;
            }
        }
    }
}
