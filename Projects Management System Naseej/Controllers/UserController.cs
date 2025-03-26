using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.UserDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using System.Security.Claims;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly MyDbContext _context;
        public UserController(IUserRepository userRepository, MyDbContext context)
        {
            _userRepository = userRepository;
            _context = context;

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving users.");
            }
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDTO>> GetUserById(int userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving the user.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<UserDTO>> CreateUser(CreateUser userDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdUser = await _userRepository.CreateUserAsync(userDTO);
                return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.UserId }, createdUser);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        [HttpPut("{userId}")]
        public async Task<ActionResult<UserDTO>> UpdateUser(int userId, UpdateUser userDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedUser = await _userRepository.UpdateUserAsync(userId, userDTO);
                if (updatedUser == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the user.");
            }
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ChangePasswordRequest passwordResetDto)
        {
            try
            {
                // Validate input
                if (passwordResetDto == null)
                {
                    return BadRequest(new { Message = "Request body is empty" });
                }

                // Log received data (be careful in production)

                // Validate input fields
                if (string.IsNullOrEmpty(passwordResetDto.CurrentPassword) ||
                    string.IsNullOrEmpty(passwordResetDto.NewPassword) ||
                    string.IsNullOrEmpty(passwordResetDto.ConfirmPassword))
                {
                    return BadRequest(new { Message = "All password fields are required" });
                }

                // Find the user
                var user = await _context.Users.FindAsync(passwordResetDto.UserId);
                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                // Hash current password for comparison
                string hashedCurrentPassword = HashPassword(passwordResetDto.CurrentPassword);

                // Verify current password
                if (user.PasswordHash != hashedCurrentPassword)
                {
                    return BadRequest(new { Message = "Current password is incorrect" });
                }

                // Validate new password
                if (passwordResetDto.NewPassword != passwordResetDto.ConfirmPassword)
                {
                    return BadRequest(new { Message = "New passwords do not match" });
                }

                // Hash new password
                string newHashedPassword = HashPassword(passwordResetDto.NewPassword);

                // Update password
                user.PasswordHash = newHashedPassword;
                user.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                // Log the full exception

                return StatusCode(500, new { Message = ex.Message });
            }
        }
        // Password hashing method
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }



        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                await _userRepository.DeleteUserAsync(userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the user.");
            }
        }

   
        [HttpGet("{userId}/roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(int userId)
        {
            try
            {
                var roles = await _userRepository.GetUserRolesAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving user roles.");
            }
        }

        [HttpPost("{userId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRoleToUser(int userId, int roleId)
        {
            try
            {
                // In a real application, you would need to get the current user's ID from the context
                int assignedBy = userId; // Placeholder for the current user's ID

                await _userRepository.AssignRoleToUserAsync(userId, roleId, assignedBy);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while assigning the role to the user.");
            }
        }

        [HttpPut("{userId}/roles")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDto model)
        {
            try
            {
                bool isUpdated = await _userRepository.UpdateUserRoleAsync(userId, model.OldRoleId, model.NewRoleId);

                if (!isUpdated)
                {
                    return NotFound($"Old role {model.OldRoleId} not found for user {userId}");
                }

                return Ok(new
                {
                    Message = "User role updated successfully",
                    UserId = userId,
                    NewRoleId = model.NewRoleId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the user role.");
            }
        }


        [HttpDelete("{userId}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(int userId, int roleId)
        {
            try
            {
                await _userRepository.RemoveRoleFromUserAsync(userId, roleId);
                return Ok(new { Message = "Role removed and replaced with Viewer role" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while removing the role from the user.");
            }
        }

        [HttpGet("greeting")]
        public IActionResult GetGreeting()
        {
            var currentTime = DateTime.UtcNow;
            string greeting;

            if (currentTime.Hour < 12)
            {
                greeting = "Good Morning";
            }
            else if (currentTime.Hour < 17)
            {
                greeting = "Good Afternoon";
            }
            else
            {
                greeting = "Good Evening";
            }

            return Ok(new
            {
                Greeting = greeting,
                CurrentTime = currentTime
            });
        }
    }
}

