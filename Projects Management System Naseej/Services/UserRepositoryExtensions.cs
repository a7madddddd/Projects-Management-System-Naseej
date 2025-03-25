using Microsoft.EntityFrameworkCore;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using System.Security.Cryptography;

namespace Projects_Management_System_Naseej.Services
{
    public static class UserRepositoryExtensions
    {
        public static async Task<User> GetOrCreateGoogleUserAsync(
            this IUserRepository userRepository,
            MyDbContext context,
            string email,
            string name)
        {
            // Try to find existing user by email
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                return existingUser;
            }

            // Generate a unique username
            string username = GenerateUniqueUsername(context, name, email);

            // Create a new user
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = GenerateRandomPassword()
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();

            return newUser;
        }

        private static string GenerateUniqueUsername(MyDbContext context, string name, string email)
        {
            // Remove spaces and convert to lowercase
            string baseUsername = name.Replace(" ", "").ToLower();

            // If base username exists, append numbers
            string username = baseUsername;
            int counter = 1;
            while (context.Users.Any(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter ++;
            }

            return username;
        }

        private static string GenerateRandomPassword()
        {
            // Generate a secure random password
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }
    }
}
