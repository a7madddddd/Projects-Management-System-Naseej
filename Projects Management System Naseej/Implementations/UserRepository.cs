using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Projects_Management_System_Naseej.DTOs.FileDTOs;
using Projects_Management_System_Naseej.DTOs.UserDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Projects_Management_System_Naseej.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly MyDbContext _context;

        public UserRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            return await _context.Users
                .Select(u => new UserDTO
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsActive = u.IsActive ?? false,
                    PasswordHash = u.PasswordHash,
                    CreatedDate = u.CreatedDate ?? DateTime.MinValue,
                    UpdatedDate = u.UpdatedDate,
                    Roles = u.UserRoleUsers.Select(ur => ur.Role.RoleName).ToList()
                })
                .ToListAsync();
        }

        public async Task<UserDTO> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            return new UserDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive ?? false,
                PasswordHash = user.PasswordHash,
                CreatedDate = user.CreatedDate ?? DateTime.MinValue,
                UpdatedDate = user.UpdatedDate,
                Roles = user.UserRoleUsers.Select(ur => ur.Role.RoleName).ToList()
            };
        }

        public async Task<UserDTO> CreateUserAsync(CreateUser userDTO)
        {
            var user = new User
            {
                Username = userDTO.Username,
                Email = userDTO.Email,
                PasswordHash = HashPassword(userDTO.Password),
                FirstName = userDTO.FirstName,
                LastName = userDTO.LastName,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive ?? false,
                PasswordHash = user.PasswordHash,
                CreatedDate = user.CreatedDate ?? DateTime.MinValue,
                UpdatedDate = user.UpdatedDate,
                Roles = new List<string>() 
            };
        }

        public async Task<UserDTO> UpdateUserAsync(int userId, UpdateUser userDTO)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            user.Username = userDTO.Username ?? user.Username;
            user.Email = userDTO.Email ?? user.Email;
            user.FirstName = userDTO.FirstName ?? user.FirstName;
            user.LastName = userDTO.LastName ?? user.LastName;
            user.IsActive = userDTO.IsActive ?? user.IsActive;
            user.UpdatedDate = DateTime.Now;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new UserDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive ?? false,
                CreatedDate = user.CreatedDate ?? DateTime.MinValue,
                UpdatedDate = user.UpdatedDate,
                Roles = user.UserRoleUsers.Select(ur => ur.Role.RoleName).ToList()
            };
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoleUsers) // تضمين الأدوار المرتبطة
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            // حذف جميع الأدوار المرتبطة بالمستخدم أولاً
            _context.UserRoles.RemoveRange(user.UserRoleUsers);

            // حذف المستخدم نفسه
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
        }



        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();
        }

        public async Task AssignRoleToUserAsync(int userId, int roleId, int assignedBy)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedDate = DateTime.Now,
                AssignedBy = assignedBy
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, int oldRoleId, int newRoleId)
        {
            try
            {
                var userRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == oldRoleId);

                if (userRole == null)
                {
                    return false;
                }

                userRole.RoleId = newRoleId;

                await _context.SaveChangesAsync();
                return true; 
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task RemoveRoleFromUserAsync(int userId, int roleId)
        {
            try
            {
                // العثور على العلاقة بين المستخدم والدور
                var userRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null)
                {
                    return;
                }

                var viewerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Viewer");

                if (viewerRole == null)
                {
                    throw new Exception("Viewer role not found");
                }

                // حذف الدور الحالي
                _context.UserRoles.Remove(userRole);

                // إضافة دور Viewer
                var newUserRole = new UserRole
                {
                    UserId = userId,
                    RoleId = viewerRole.RoleId
                };
                _context.UserRoles.Add(newUserRole);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<UserDTO> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user == null) return null;

            return new UserDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive ?? false,
                PasswordHash = user.PasswordHash,
                CreatedDate = user.CreatedDate ?? DateTime.MinValue,
                UpdatedDate = user.UpdatedDate,
                Roles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.UserId)
                    .Select(ur => ur.Role.RoleName)
                    .ToListAsync()
            };
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }



        public async Task<User> GetOrCreateUserFromGoogleAsync(string email, string name)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            try
            {
                // Try to find existing user by email
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (existingUser != null)
                {
                    return existingUser;
                }

                // Generate a unique username
                string username = GenerateUniqueUsername(name, email);

                // Create a new user
                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = GenerateRandomPassword()
                };

                // Add default role if needed
                await AddDefaultRoleToUser(newUser);

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();


                return newUser;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GenerateUniqueUsername(string name, string email)
        {
            // Remove spaces and convert to lowercase
            string baseUsername = string.IsNullOrEmpty(name)
                ? email.Split('@')[0].ToLower()
                : name.Replace(" ", "").ToLower();

            string username = baseUsername;
            int counter = 1;

            // Ensure username is unique
            while (_context.Users.Any(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }

        private string GenerateRandomPassword()
        {
            // Generate a secure random password
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }

        private async Task AddDefaultRoleToUser(User user)
        {
            try
            {
                // Find the default role (e.g., "Viewer")
                var defaultRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == "viewer");

                if (defaultRole != null)
                {
                    // Create a user role entry
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = defaultRole.RoleId
                    };

                    _context.UserRoles.Add(userRole);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }


    }
}

