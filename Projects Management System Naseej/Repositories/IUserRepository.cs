using Projects_Management_System_Naseej.DTOs.UserDTOs;
using Projects_Management_System_Naseej.Models;

namespace Projects_Management_System_Naseej.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();
        Task<UserDTO> GetUserByIdAsync(int userId);
        Task<UserDTO> CreateUserAsync(CreateUser userDTO);
        Task<UserDTO> UpdateUserAsync(int userId, UpdateUser userDTO);
        Task DeleteUserAsync(int userId);
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);
        Task AssignRoleToUserAsync(int userId, int roleId, int assignedBy);
        Task RemoveRoleFromUserAsync(int userId, int roleId);
        Task<UserDTO> GetUserByUsernameOrEmailAsync(string usernameOrEmail);
    }
}
