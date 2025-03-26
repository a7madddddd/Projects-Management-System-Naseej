using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Projects_Management_System_Naseej
{
    public static class TokenGenerator
    {
        // Method to generate token with configuration passed
        public static string GeneratePasswordResetToken(string email, IConfiguration configuration)
        {
            // Generate a secure, time-limited token
            var tokenHandler = new JwtSecurityTokenHandler();

            // Ensure you have Jwt:SecretKey in your configuration
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT Secret Key is not configured"));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(JwtRegisteredClaimNames.Exp,
                        DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds().ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Method to validate token with configuration passed
        public static bool ValidatePasswordResetToken(string token, IConfiguration configuration)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(configuration["Jwt:SecretKey"]
                    ?? throw new InvalidOperationException("JWT Secret Key is not configured"));

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }
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