using System.Security.Cryptography;
using System.Text;

namespace PPE.Utility
{
    public static class Crypto
    {
        private static readonly string key = "Q31JZWdbT3cxbGJTWSd1dUdHe0otelBj";

        private static byte[] GetKey()
        {
            string password = Encoding.UTF8.GetString(Convert.FromBase64String(key));
            string pwd = password.Length < 24 ? password.PadRight(24) : password[..24];
            return Encoding.UTF8.GetBytes(pwd);
        }


        /// Encrypter strings 3DES (192 bits)
        public static string Encrypt(string data)
        {
            using TripleDES DES = TripleDES.Create();
            DES.Mode = CipherMode.ECB;
            DES.Key = GetKey();
            DES.Padding = PaddingMode.PKCS7;

            ICryptoTransform DESEncrypt = DES.CreateEncryptor();
            byte[] Buffer = Encoding.ASCII.GetBytes(data);

            return Convert.ToBase64String(DESEncrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));
        }


        /// Decrypter strings 3DES (192 bits)
        public static string Decrypt(string data)
        {
            using TripleDES DES = TripleDES.Create();
            DES.Mode = CipherMode.ECB;
            DES.Key = GetKey();
            DES.Padding = PaddingMode.PKCS7;

            ICryptoTransform DESEncrypt = DES.CreateDecryptor();
            byte[] Buffer = Convert.FromBase64String(data.Replace(" ", "+"));

            return Encoding.UTF8.GetString(DESEncrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));
        }
    }
}
