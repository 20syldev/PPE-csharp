using Npgsql;
using System.Text.RegularExpressions;
using PPE.Utility;

namespace PPE.Model
{
    /// <summary>
    /// Password validation result
    /// </summary>
    public class PasswordValidation
    {
        public bool IsValid { get; set; }
        public string Strength { get; set; } = "Weak";
        public string Color { get; set; } = "#F87171";
        public List<string> Errors { get; set; } = [];

        public bool HasMinLength { get; set; }
        public bool HasUppercase { get; set; }
        public bool HasLowercase { get; set; }
        public bool HasDigit { get; set; }
        public bool HasSpecialChars { get; set; }
        public bool NoConsecutiveRepeat { get; set; }
    }

    /// <summary>
    /// Email validation result
    /// </summary>
    public class EmailValidation
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Postal code validation result
    /// </summary>
    public class PostalCodeValidation
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// User entity class with authentication and personal information
    /// Uses stored procedure for auto-incremented id_code retrieval
    /// SHA512 hash + salt for passwords
    /// </summary>
    public class User
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

        // 2FA properties
        public string? TotpSecret { get; set; }
        public bool TotpEnabled { get; set; }
        public string? RecoveryCodes { get; set; }

        // Currently logged in user
        public static User? Current { get; set; }

        public User() { }

        public User(string login)
        {
            Login = login;
        }

        public User(string nom, string adresse, string ville, string code)
        {
            Nom = nom;
            Adresse = adresse;
            Ville = ville;
            Code = code;
        }

        /// <summary>
        /// Validates an email (format and valid domain)
        /// </summary>
        public static EmailValidation ValidateEmail(string email)
        {
            var result = new EmailValidation();

            if (string.IsNullOrWhiteSpace(email))
            {
                result.Error = "Email cannot be empty";
                return result;
            }

            // Regex to validate email format with domain (name.extension)
            var emailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, emailRegex))
            {
                result.Error = "Invalid email format (e.g. name@domain.com)";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Validates a French postal code (5 digits)
        /// Accepted formats: 01000-95999 (metropolitan), 20000-20999 (Corsica), 97xxx-98xxx (overseas)
        /// </summary>
        public static PostalCodeValidation ValidatePostalCode(string postalCode)
        {
            var result = new PostalCodeValidation();

            if (string.IsNullOrWhiteSpace(postalCode))
            {
                result.Error = "Postal code cannot be empty";
                return result;
            }

            // Must be exactly 5 digits
            if (!Regex.IsMatch(postalCode, @"^\d{5}$"))
            {
                result.Error = "Postal code must contain 5 digits";
                return result;
            }

            int code = int.Parse(postalCode);

            // French postal code range validation:
            // 01000-19999: Departments 01-19
            // 20000-20999: Corsica (2A and 2B)
            // 21000-95999: Departments 21-95
            // 97100-97699: Overseas departments (Guadeloupe, Martinique, French Guiana, Reunion, Mayotte)
            // 98000-98899: Overseas collectivities (Monaco, etc.)
            bool isValid = (code >= 1000 && code <= 95999) ||  // Metropolitan + Corsica
                          (code >= 97100 && code <= 97699) ||  // Overseas departments
                          (code >= 98000 && code <= 98899);    // Overseas collectivities

            if (!isValid)
            {
                result.Error = "Invalid French postal code";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Validates a password according to criteria:
        /// - Minimum 8 characters
        /// - At least 2 special characters
        /// - At least one uppercase letter
        /// - At least one lowercase letter
        /// - At least one digit
        /// - No character repeated twice in a row
        /// </summary>
        public static PasswordValidation ValidatePassword(string password)
        {
            var result = new PasswordValidation();

            if (string.IsNullOrEmpty(password))
            {
                result.Errors.Add("Password cannot be empty");
                return result;
            }

            result.HasMinLength = password.Length >= 8;
            if (!result.HasMinLength)
                result.Errors.Add("Minimum 8 characters");

            result.HasUppercase = Regex.IsMatch(password, @"[A-Z]");
            if (!result.HasUppercase)
                result.Errors.Add("At least one uppercase letter");

            result.HasLowercase = Regex.IsMatch(password, @"[a-z]");
            if (!result.HasLowercase)
                result.Errors.Add("At least one lowercase letter");

            result.HasDigit = Regex.IsMatch(password, @"[0-9]");
            if (!result.HasDigit)
                result.Errors.Add("At least one digit");

            var specialCount = Regex.Matches(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]").Count;
            result.HasSpecialChars = specialCount >= 2;
            if (!result.HasSpecialChars)
                result.Errors.Add($"At least 2 special characters ({specialCount}/2)");

            result.NoConsecutiveRepeat = !Regex.IsMatch(password, @"(.)\1\1");
            if (!result.NoConsecutiveRepeat)
                result.Errors.Add("No character repeated 3 times in a row");

            result.IsValid = result.HasMinLength &&
                            result.HasUppercase &&
                            result.HasLowercase &&
                            result.HasDigit &&
                            result.HasSpecialChars &&
                            result.NoConsecutiveRepeat;

            if (!result.IsValid)
            {
                result.Strength = "Weak";
                result.Color = "#F87171";
            }
            else if (password.Length >= 16)
            {
                result.Strength = "Very Strong";
                result.Color = "#10B981";
            }
            else if (password.Length >= 12)
            {
                result.Strength = "Strong";
                result.Color = "#34D399";
            }
            else
            {
                result.Strength = "Fair";
                result.Color = "#FBBF24";
            }

            return result;
        }

        /// <summary>
        /// Calculates the strength percentage for the progress bar
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
        /// Checks if a login already exists
        /// </summary>
        public static bool LoginExists(string login)
        {
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("SELECT utilisateur_login_exists(@login)", db.DbConnection);
                cmd.Parameters.AddWithValue("@login", login);

                var result = cmd.ExecuteScalar();
                return result != null && (bool)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking login: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Creates a new user account with SHA512 hash + salt
        /// </summary>
        public bool CreateAccount(string password)
        {
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return false;
                if (string.IsNullOrEmpty(Login)) return false;

                if (LoginExists(Login))
                {
                    Console.WriteLine("Error: this login already exists");
                    return false;
                }

                var validation = ValidatePassword(password);
                if (!validation.IsValid)
                {
                    Console.WriteLine("Error: invalid password");
                    return false;
                }

                Id = Guid.NewGuid();
                Password = Hashing.HashPassword(password);

                using var cmd = new NpgsqlCommand("CALL utilisateur_add(@id, @login, @password, @admin, @nom, @adresse, @ville, @code, @id_code)", db.DbConnection);
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
                Console.WriteLine("Error creating account: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Authenticates a user with login and password
        /// </summary>
        public static User? Authenticate(string login, string password)
        {
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return null;

                using var cmd = new NpgsqlCommand("SELECT * FROM utilisateur_get_by_login(@login)", db.DbConnection);
                cmd.Parameters.AddWithValue("@login", login);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var user = new User
                    {
                        Id = reader.GetGuid(0),
                        IdCode = reader.GetInt32(1),
                        Login = reader.GetString(2),
                        Password = reader.GetString(3),
                        Admin = reader.GetBoolean(4),
                        Nom = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Adresse = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Ville = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Code = reader.IsDBNull(8) ? null : reader.GetString(8),
                        TotpSecret = reader.IsDBNull(9) ? null : reader.GetString(9),
                        TotpEnabled = !reader.IsDBNull(10) && reader.GetBoolean(10),
                        RecoveryCodes = reader.IsDBNull(11) ? null : reader.GetString(11)
                    };

                    if (Hashing.VerifyPassword(password, user.Password!))
                    {
                        Current = user;
                        return user;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during authentication: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Lists all users
        /// </summary>
        public static List<User> ListAll()
        {
            var users = new List<User>();
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return users;

                using var cmd = new NpgsqlCommand("SELECT * FROM utilisateur_list()", db.DbConnection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = reader.GetGuid(0),
                        IdCode = reader.GetInt32(1),
                        Login = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Password = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Admin = reader.GetBoolean(4),
                        Nom = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Adresse = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Ville = reader.IsDBNull(7) ? null : reader.GetString(7),
                        Code = reader.IsDBNull(8) ? null : reader.GetString(8),
                        TotpSecret = reader.IsDBNull(9) ? null : reader.GetString(9),
                        TotpEnabled = !reader.IsDBNull(10) && reader.GetBoolean(10),
                        RecoveryCodes = reader.IsDBNull(11) ? null : reader.GetString(11)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading users: " + ex.Message);
            }

            return users;
        }

        /// <summary>
        /// Updates user personal information
        /// </summary>
        public bool Update()
        {
            if (Id == null) return false;
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL utilisateur_update(@id, @nom, @adresse, @ville, @code)", db.DbConnection);
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
                Console.WriteLine("Error updating user: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates user 2FA settings
        /// </summary>
        public bool Update2FA()
        {
            if (Id == null) return false;
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL utilisateur_update_2fa(@id, @totp_secret, @totp_enabled, @recovery_codes)", db.DbConnection);
                cmd.Parameters.AddWithValue("@id", Id.Value);
                cmd.Parameters.AddWithValue("@totp_secret", (object?)TotpSecret ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@totp_enabled", TotpEnabled);
                cmd.Parameters.AddWithValue("@recovery_codes", (object?)RecoveryCodes ?? DBNull.Value);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating 2FA: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        public static bool Delete(Guid id)
        {
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand("CALL utilisateur_delete(@id)", db.DbConnection);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting user: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Changes user password with verification of last 3 passwords history
        /// </summary>
        /// <returns>0 = success, 1 = password in history, 2 = error</returns>
        public int ChangePassword(string newPassword)
        {
            if (Id == null) return 2;
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return 2;

                // Validate new password
                var validation = ValidatePassword(newPassword);
                if (!validation.IsValid) return 2;

                // Hash new password
                var newPasswordHash = Hashing.HashPassword(newPassword);

                // Call SQL function that verifies history and changes password
                using var cmd = new NpgsqlCommand("SELECT utilisateur_change_password(@user_id, @new_password_hash)", db.DbConnection);
                cmd.Parameters.AddWithValue("@user_id", Id.Value);
                cmd.Parameters.AddWithValue("@new_password_hash", newPasswordHash);

                var result = cmd.ExecuteScalar();
                var returnCode = result != null ? Convert.ToInt32(result) : 2;

                // If success, update local password
                if (returnCode == 0)
                {
                    Password = newPasswordHash;
                }

                return returnCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error changing password: " + ex.Message);
                return 2;
            }
        }

        /// <summary>
        /// Gets the date of the last password change from password_history
        /// </summary>
        public DateTime? GetLastPasswordChange()
        {
            if (Id == null) return null;
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return null;

                using var cmd = new NpgsqlCommand(
                    "SELECT created_at FROM password_history WHERE user_id = @user_id ORDER BY created_at DESC LIMIT 1",
                    db.DbConnection);
                cmd.Parameters.AddWithValue("@user_id", Id.Value);

                var result = cmd.ExecuteScalar();
                return result as DateTime?;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting last password change: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks if a password matches the current password or any of the last 3 passwords in history
        /// </summary>
        public bool IsPasswordInHistory(string password)
        {
            // Check current password
            if (Hashing.VerifyPassword(password, Password ?? ""))
                return true;

            if (Id == null) return false;
            var db = Connection.Instance();

            try
            {
                if (!db.IsConnect()) return false;

                using var cmd = new NpgsqlCommand(
                    "SELECT password_hash FROM password_history WHERE user_id = @user_id ORDER BY created_at DESC LIMIT 3",
                    db.DbConnection);
                cmd.Parameters.AddWithValue("@user_id", Id.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var hash = reader.GetString(0);
                    if (Hashing.VerifyPassword(password, hash))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking password history: " + ex.Message);
                return false;
            }
        }
    }
}
