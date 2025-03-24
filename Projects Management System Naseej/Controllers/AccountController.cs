﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Projects_Management_System_Naseej.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Projects_Management_System_Naseej.Controllers.FilesController;
using Microsoft.AspNetCore.Diagnostics;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Services;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly MyDbContext _context;
        public AccountController(IUserRepository userRepository, IRoleRepository roleRepository, IConfiguration configuration , ILogger<AccountController> logger, MyDbContext context)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTOs.LoginDTOs.LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetUserByUsernameOrEmailAsync(loginDTO.UsernameOrEmail);
            if (user != null && user.PasswordHash == HashPassword(loginDTO.Password))
            {
                var userRoles = await _userRepository.GetUserRolesAsync(user.UserId);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim("UserId", user.UserId.ToString())
                };

                // Add role claims
                foreach (var role in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(8),
                    signingCredentials: creds);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    userId = user.UserId,
                    username = user.Username,
                    roles = userRoles
                });
            }

            return Unauthorized("Invalid username, email, or password.");
        }


        [HttpPost("{userId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRoleToUser(int userId, int roleId)
        {
            try
            {
                // In a real application, you would need to get the current user's ID from the context
                var currentUserIdClaim = User.FindFirst("UserId");
                if (currentUserIdClaim == null)
                {
                    return Unauthorized("User ID not found in the token.");
                }

                if (!int.TryParse(currentUserIdClaim.Value, out int assignedBy))
                {
                    return BadRequest("Invalid user ID in the token.");
                }

                await _userRepository.AssignRoleToUserAsync(userId, roleId, assignedBy);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while assigning the role to the user.");
            }
        }


        [HttpGet("login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                // Use absolute URL for RedirectUri
                RedirectUri = Url.Action("GoogleCallback", "Account", null, Request.Scheme, Request.Host.ToString())
            };

            // Optional: Add specific parameters
            properties.Items["prompt"] = "select_account"; // Force account selection

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        private string GenerateCodeVerifier()
        {
            var randomBytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(challengeBytes)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
            }
        }




        [HttpGet("login-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var authResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (!authResult.Succeeded)
                {
                    _logger.LogError("Google authentication failed");
                    return Unauthorized("Authentication failed");
                }

                var claims = authResult.Principal.Identities.First().Claims;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                // Use the extension method with both _userRepository and _context
                var user = await _userRepository.GetOrCreateGoogleUserAsync(_context, email, name);

                // Generate your internal JWT token
                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    token,
                    email,
                    name,
                    userId = user.UserId,
                    message = "Authentication successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Callback error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Your existing GenerateJwtToken method...
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username)
        };

            // Add user roles if needed
            var userRoles = _userRepository.GetUserRolesAsync(user.UserId).Result;
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }





        [HttpGet("check-google-config")]
        public IActionResult CheckGoogleConfig()
        {
            return Ok(new
            {
                ClientId = _configuration["Google:ClientId"],
                ClientSecret = _configuration["Google:ClientSecret"] != null ? "**HIDDEN**" : "NOT SET",
                CallbackPath = "/api/Account/login-callback",
                CurrentScheme = Request.Scheme,
                CurrentHost = Request.Host.ToString(),
                RedirectUris = new[]
                {
            "",
            ""
        }
            });
        }


        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}