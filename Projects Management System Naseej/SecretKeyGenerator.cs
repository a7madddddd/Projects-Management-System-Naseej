using System;
using System.Security.Cryptography;
using System.Text;

namespace Projects_Management_System_Naseej
{
    public class SecretKeyGenerator
    {
        public static string GenerateSecretKey(int keySize = 32) // 32 bytes = 256 bits
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var keyBytes = new byte[keySize];
                rng.GetBytes(keyBytes);
                return Convert.ToBase64String(keyBytes);
            }
        }

        public static void Main(string[] args)
        {
            var secretKey = GenerateSecretKey();
            Console.WriteLine($"Generated Secret Key: {secretKey}");
        }
    }
}