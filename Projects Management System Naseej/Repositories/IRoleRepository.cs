using Projects_Management_System_Naseej.DTOs.RoleDTOs;
using Projects_Management_System_Naseej.Models;

namespace Projects_Management_System_Naseej.Repositories
{
    public interface IRoleRepository
    {
        Task<IEnumerable<RoleDTO>> GetAllRolesAsync();
        Task<RoleDTO> GetRoleByIdAsync(int roleId);
        Task<RoleDTO> CreateRoleAsync(CreateRoleDTO roleDTO);
        Task<RoleDTO> UpdateRoleAsync(int roleId, UpdateRoleDTO roleDTO);
        Task DeleteRoleAsync(int roleId);
    }
}
