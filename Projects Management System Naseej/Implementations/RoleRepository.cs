using Microsoft.EntityFrameworkCore;
using Projects_Management_System_Naseej.DTOs.RoleDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Implementations
{
    public class RoleRepository : IRoleRepository
    {
        private readonly MyDbContex _context;

        public RoleRepository(MyDbContex context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoleDTO>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Select(r => new RoleDTO
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description
                })
                .ToListAsync();
        }

        public async Task<RoleDTO> GetRoleByIdAsync(int roleId)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return null;

            return new RoleDTO
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description
            };
        }

        public async Task<RoleDTO> CreateRoleAsync(CreateRoleDTO roleDTO)
        {
            var role = new Role
            {
                RoleName = roleDTO.RoleName,
                Description = roleDTO.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return new RoleDTO
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description
            };
        }

        public async Task<RoleDTO> UpdateRoleAsync(int roleId, UpdateRoleDTO roleDTO)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return null;

            role.RoleName = roleDTO.RoleName ?? role.RoleName;
            role.Description = roleDTO.Description ?? role.Description;

            _context.Roles.Update(role);
            await _context.SaveChangesAsync();

            return new RoleDTO
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description
            };
        }

        public async Task DeleteRoleAsync(int roleId)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }
    }
}

