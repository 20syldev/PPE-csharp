using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using PPE.Utility;

namespace PPE.Model
{
    /// <summary>
    /// Service for TOTP (Two-Factor Authentication) management
    /// </summary>
    public static class TotpService
    {
        private const int SecretLength = 20;
        private const int RecoveryCodeCount = 8;
        private const int RecoveryCodeLength = 8;
        private const string Issuer = "PPE";

        /// <summary>
        /// Generates a random TOTP secret key
        /// </summary>
        public static string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(SecretLength);
            return Base32Encoding.ToString(key);
        }

        /// <summary>
        /// Validates a TOTP code
        /// </summary>
        public static bool ValidateCode(string secret, string code)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(secretBytes);
                // Allow ±2 steps (±60 seconds) for time drift
                var window = new VerificationWindow(previous: 2, future: 2);
                var result = totp.VerifyTotp(code, out long timeStepMatched, window);
                Console.WriteLine($"[2FA] Code validation: {result}, TimeStep: {timeStepMatched}, Expected: {totp.ComputeTotp()}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[2FA] Validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates the otpauth URI for authenticator apps
        /// </summary>
        public static string GenerateOtpAuthUri(string secret, string userEmail)
        {
            var encodedIssuer = Uri.EscapeDataString(Issuer);
            var encodedEmail = Uri.EscapeDataString(userEmail);
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        /// <summary>
        /// Generates a QR code as byte array (PNG)
        /// </summary>
        public static byte[] GenerateQrCode(string otpAuthUri)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(5);
        }

        /// <summary>
        /// Generates 8 random recovery codes
        /// </summary>
        public static List<string> GenerateRecoveryCodes()
        {
            var codes = new List<string>();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            for (int i = 0; i < RecoveryCodeCount; i++)
            {
                var code = new char[RecoveryCodeLength];
                var bytes = RandomNumberGenerator.GetBytes(RecoveryCodeLength);
                for (int j = 0; j < RecoveryCodeLength; j++)
                {
                    code[j] = chars[bytes[j] % chars.Length];
                }
                codes.Add(new string(code));
            }

            return codes;
        }

        /// <summary>
        /// Validates a recovery code and removes it from the list
        /// </summary>
        public static bool ValidateAndConsumeRecoveryCode(string code, ref List<string> codes)
        {
            var upperCode = code.ToUpperInvariant().Replace("-", "").Replace(" ", "");
            var index = codes.FindIndex(c => c.Equals(upperCode, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                codes.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Encrypts and formats recovery codes for storage
        /// </summary>
        public static string EncryptRecoveryCodes(List<string> codes)
        {
            var joined = string.Join(",", codes);
            return Crypto.Encrypt(joined);
        }

        /// <summary>
        /// Decrypts and parses recovery codes
        /// </summary>
        public static List<string> DecryptRecoveryCodes(string? encryptedCodes)
        {
            if (string.IsNullOrEmpty(encryptedCodes))
                return new List<string>();

            try
            {
                var decrypted = Crypto.Decrypt(encryptedCodes);
                return decrypted.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Encrypts the TOTP secret for storage
        /// </summary>
        public static string EncryptSecret(string secret)
        {
            return Crypto.Encrypt(secret);
        }

        /// <summary>
        /// Decrypts the TOTP secret
        /// </summary>
        public static string DecryptSecret(string? encryptedSecret)
        {
            if (string.IsNullOrEmpty(encryptedSecret))
                return string.Empty;

            return Crypto.Decrypt(encryptedSecret);
        }
    }
}
