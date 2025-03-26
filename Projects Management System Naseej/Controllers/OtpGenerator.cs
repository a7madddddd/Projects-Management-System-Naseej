using System.Security.Cryptography;

namespace Projects_Management_System_Naseej.Controllers
{
    internal static class OtpGenerator
    {
        public static string GenerateOtp(int length = 6)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[length];
                rng.GetBytes(randomBytes);

                return string.Join("", randomBytes.Select(b => (b % 10).ToString()));
            }
        }
    }
}