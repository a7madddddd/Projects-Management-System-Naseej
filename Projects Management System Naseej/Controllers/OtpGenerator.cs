using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

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

        public static string GeneratePasswordResetToken(string email, IConfiguration configuration)
        {
            // Retrieve JWT configuration
            var jwtKey = configuration["Jwt:Key"];
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];

            // Validate JWT configuration
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured");
            }

            // Ensure the key is at least 32 characters long and base64 encoded
            var key = Encoding.UTF8.GetBytes(jwtKey);
            if (key.Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 32 bytes long");
            }

            // Create security key
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create claims
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("purpose", "password-reset")
            };

            // Create token
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // Token valid for 15 minutes
                signingCredentials: credentials
            );

            // Return token as string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}