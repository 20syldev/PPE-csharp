using Npgsql;
using System.Text.RegularExpressions;

namespace PPE.Modele
{
    /// <summary>
    /// Résultat de la validation du mot de passe
    /// </summary>
    public class PasswordValidation
    {
        public bool IsValid { get; set; }
        public string Strength { get; set; } = "Faible";
        public string Color { get; set; } = "#F87171"; // Rouge
        public List<string> Errors { get; set; } = [];

        public bool HasMinLength { get; set; }
        public bool HasUppercase { get; set; }
        public bool HasLowercase { get; set; }
        public bool HasDigit { get; set; }
        public bool HasSpecialChars { get; set; }
        public bool NoConsecutiveRepeat { get; set; }
    }

    /// <summary>
    /// Résultat de la validation de l'email
    /// </summary>
    public class EmailValidation
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Résultat de la validation du code postal
    /// </summary>
    public class CodePostalValidation
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Classe métier Utilisateur avec authentification et informations personnelles
    /// Utilise une procédure stockée pour la récupération de l'id_code auto-incrémenté
    /// Hash SHA512 + salage pour les mots de passe
    /// </summary>
    public class Utilisateur
    {
        public Guid? Id { get; set; }
        public int? IdCode { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public bool Admin { get; set; }
        public string? Nom { get; set; }
        public string? Adresse { get; set; }
        public string? Ville { get; set; }
        public string? Code { get; set; }

        // Utilisateur actuellement connecté
        public static Utilisateur? Current { get; set; }

        public Utilisateur() { }

        public Utilisateur(string login)
        {
            Login = login;
        }

        public Utilisateur(string nom, string adresse, string ville, string code)
        {
            Nom = nom;
            Adresse = adresse;
            Ville = ville;
            Code = code;
        }

        /// <summary>
        /// Valide un email (format et domaine valide)
        /// </summary>
        public static EmailValidation ValidateEmail(string email)
        {
            var result = new EmailValidation();

            if (string.IsNullOrWhiteSpace(email))
            {
                result.Error = "L'email ne peut pas être vide";
                return result;
            }

            // Regex pour valider le format email avec domaine (nom.extension)
            var emailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, emailRegex))
            {
                result.Error = "Format d'email invalide (ex: nom@domaine.com)";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Valide un code postal français (5 chiffres)
        /// Formats acceptés: 01000-95999 (métropole), 20000-20999 (Corse), 97xxx-98xxx (DOM-TOM)
        /// </summary>
        public static CodePostalValidation ValidateCodePostal(string codePostal)
        {
            var result = new CodePostalValidation();

            if (string.IsNullOrWhiteSpace(codePostal))
            {
                result.Error = "Le code postal ne peut pas être vide";
                return result;
            }

            // Doit être exactement 5 chiffres
            if (!Regex.IsMatch(codePostal, @"^\d{5}$"))
            {
                result.Error = "Le code postal doit contenir 5 chiffres";
                return result;
            }

            int code = int.Parse(codePostal);

            // Validation des plages françaises:
            // 01000-19999: Départements 01-19
            // 20000-20999: Corse (2A et 2B)
            // 21000-95999: Départements 21-95
            // 97100-97699: DOM (Guadeloupe, Martinique, Guyane, Réunion, Mayotte)
            // 98000-98899: COM (Monaco, etc.)
            bool isValid = (code >= 1000 && code <= 95999) ||  // Métropole + Corse
                          (code >= 97100 && code <= 97699) ||  // DOM
                          (code >= 98000 && code <= 98899);    // COM

            if (!isValid)
            {
                result.Error = "Code postal français invalide";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Valide un mot de passe selon les critères:
        /// - 8 caractères minimum
        /// - Au moins 2 caractères spéciaux
        /// - Au moins une majuscule
        /// - Au moins une minuscule
        /// - Au moins un chiffre
        /// - Pas de répétition de caractères 2 fois de suite
        /// </summary>
        public static PasswordValidation ValidatePassword(string password)
        {
            var result = new PasswordValidation();

            if (string.IsNullOrEmpty(password))
            {
                result.Errors.Add("Le mot de passe ne peut pas être vide");
                return result;
            }

            result.HasMinLength = password.Length >= 8;
            if (!result.HasMinLength)
                result.Errors.Add("8 caractères minimum");

            result.HasUppercase = Regex.IsMatch(password, @"[A-Z]");
            if (!result.HasUppercase)
                result.Errors.Add("Au moins une majuscule");

            result.HasLowercase = Regex.IsMatch(password, @"[a-z]");
            if (!result.HasLowercase)
                result.Errors.Add("Au moins une minuscule");

            result.HasDigit = Regex.IsMatch(password, @"[0-9]");
            if (!result.HasDigit)
                result.Errors.Add("Au moins un chiffre");

            var specialCount = Regex.Matches(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]").Count;
            result.HasSpecialChars = specialCount >= 2;
            if (!result.HasSpecialChars)
                result.Errors.Add($"Au moins 2 caractères spéciaux ({specialCount}/2)");

            result.NoConsecutiveRepeat = !Regex.IsMatch(password, @"(.)\1");
            if (!result.NoConsecutiveRepeat)
                result.Errors.Add("Pas de caractère répété 2 fois de suite");

            result.IsValid = result.HasMinLength &&
                            result.HasUppercase &&
                            result.HasLowercase &&
                            result.HasDigit &&
                            result.HasSpecialChars &&
                            result.NoConsecutiveRepeat;

            // Déterminer la force et la couleur
            if (!result.IsValid)
            {
                result.Strength = "Faible";
                result.Color = "#F87171"; // Rouge
            }
            else if (password.Length >= 16)
            {
                result.Strength = "Très fort";
                result.Color = "#10B981"; // Vert foncé
            }
            else if (password.Length >= 12)
            {
                result.Strength = "Fort";
                result.Color = "#34D399"; // Vert
            }
            else
            {
                result.Strength = "Correct";
                result.Color = "#FBBF24"; // Jaune
            }

            return result;
        }

        /// <summary>
        /// Calcule le pourcentage de progression pour la jauge
        /// </summary>
        public static int GetPasswordStrengthPercentage(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            var validation = ValidatePassword(password);
            int score = 0;

            if (validation.HasMinLength) score++;
            if (validation.HasUppercase) score++;
            if (validation.HasLowercase) score++;
            if (validation.HasDigit) score++;
            if (validation.HasSpecialChars) score++;
            if (validation.NoConsecutiveRepeat) score++;
            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;

            return (int)((double)score / 8 * 100);
        }

        /// <summary>
        /// Vérifie si un login existe déjà
        /// </summary>
        public static bool LoginExists(string login)
        {
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("SELECT utilisateur_login_exists(@login)", db.Connection);
                cmd.Parameters.AddWithValue("@login", login);

                var result = cmd.ExecuteScalar();
                return result != null && (bool)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la vérification du login : " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Crée un nouveau compte utilisateur avec hash SHA512 + salage
        /// </summary>
        public bool CreerCompte(string password)
        {
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;
                if (string.IsNullOrEmpty(Login)) return false;

                if (LoginExists(Login))
                {
                    Console.WriteLine("Erreur : ce login existe déjà");
                    return false;
                }

                var validation = ValidatePassword(password);
                if (!validation.IsValid)
                {
                    Console.WriteLine("Erreur : mot de passe invalide");
                    return false;
                }

                Id = Guid.NewGuid();
                Password = Hashage.HashPassword(password);

                using var cmd = new NpgsqlCommand("CALL utilisateur_add(@id, @login, @password, @admin, @nom, @adresse, @ville, @code, @id_code)", db.Connection);
                cmd.Parameters.AddWithValue("@id", Id.Value);
                cmd.Parameters.AddWithValue("@login", Login);
                cmd.Parameters.AddWithValue("@password", Password);
                cmd.Parameters.AddWithValue("@admin", Admin);
                cmd.Parameters.AddWithValue("@nom", Nom ?? "");
                cmd.Parameters.AddWithValue("@adresse", Adresse ?? "");
                cmd.Parameters.AddWithValue("@ville", Ville ?? "");
                cmd.Parameters.AddWithValue("@code", Code ?? "");

                var outParam = new NpgsqlParameter("@id_code", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Direction = System.Data.ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };
                cmd.Parameters.Add(outParam);

                cmd.ExecuteNonQuery();

                if (outParam.Value != DBNull.Value)
                {
                    IdCode = Convert.ToInt32(outParam.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la création du compte : " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Authentifie un utilisateur avec login et mot de passe
        /// </summary>
        public static Utilisateur? Authentifier(string login, string password)
        {
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return null;

                using var cmd = new NpgsqlCommand("SELECT * FROM utilisateur_get_by_login(@login)", db.Connection);
                cmd.Parameters.AddWithValue("@login", login);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var utilisateur = new Utilisateur
                    {
                        Id = reader.GetGuid(0),
                        IdCode = reader.GetInt32(1),
                        Login = reader.GetString(2),
                        Password = reader.GetString(3),
                        Admin = reader.GetBoolean(4),
                        Nom = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Adresse = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Ville = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Code = reader.IsDBNull(8) ? null : reader.GetString(8)
                    };

                    if (Hashage.VerifyPassword(password, utilisateur.Password!))
                    {
                        Current = utilisateur;
                        return utilisateur;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de l'authentification : " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Liste tous les utilisateurs
        /// </summary>
        public static List<Utilisateur> ListerTous()
        {
            var utilisateurs = new List<Utilisateur>();
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return utilisateurs;

                using var cmd = new NpgsqlCommand("SELECT * FROM utilisateur_list()", db.Connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    utilisateurs.Add(new Utilisateur
                    {
                        Id = reader.GetGuid(0),
                        IdCode = reader.GetInt32(1),
                        Login = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Password = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Admin = reader.GetBoolean(4),
                        Nom = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Adresse = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Ville = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Code = reader.IsDBNull(8) ? null : reader.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la lecture : " + ex.Message);
            }

            return utilisateurs;
        }

        /// <summary>
        /// Modifie les informations personnelles de l'utilisateur
        /// </summary>
        public bool Modifier()
        {
            if (Id == null) return false;
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL utilisateur_update(@id, @nom, @adresse, @ville, @code)", db.Connection);
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

        /// <summary>
        /// Supprime un utilisateur
        /// </summary>
        public static bool Supprimer(Guid id)
        {
            var db = Connect.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL utilisateur_delete(@id)", db.Connection);
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
