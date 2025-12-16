using System.Text;
using System.Security.Cryptography;

namespace PPE.Utility
{
    public class Hashing
    {
        // Generates SHA-512 hash of a string
        private static string Hash(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = SHA512.HashData(bytes);
            StringBuilder result = new();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }

        // Generates random salt
        private static string Salt(int size = 32)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(saltBytes);
        }

        // Hashes a password (returns hash:salt)
        public static string HashPassword(string password)
        {
            string salt = Salt();
            string hash = Hash(salt + password);
            return $"{hash}:{salt}";
        }

        // Verifies a password against a hash
        public static bool VerifyPassword(string password, string hashWithSalt)
        {
            string[] parts = hashWithSalt.Split(':');
            if (parts.Length != 2) return false;
            return Hash(parts[1] + password) == parts[0];
        }
    }
}
