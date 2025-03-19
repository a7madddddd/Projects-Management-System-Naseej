using System;
using System.Security.Cryptography;

public class SecretKeyGenerator
{
    public static string GenerateSecretKey(int keySize = 32)
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
        Console.WriteLine("\nPress Enter to exit and copy the key from the console.");
        Console.ReadLine();
    }
}