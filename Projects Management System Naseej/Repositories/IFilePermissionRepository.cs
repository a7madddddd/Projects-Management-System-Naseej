using Projects_Management_System_Naseej.DTOs.FilePermissionDTOs;
using Projects_Management_System_Naseej.Models;

namespace Projects_Management_System_Naseej.Repositories
{
    public interface IFilePermissionRepository
    {
        Task<IEnumerable<FilePermissionDTO>> GetAllPermissionsAsync();
        Task<FilePermissionDTO> GetPermissionByIdAsync(int permissionId);
        Task<FilePermissionDTO> CreatePermissionAsync(CreateFilePermission permissionDTO);
        Task<FilePermissionDTO> UpdatePermissionAsync(int permissionId, UpdateFilePermission permissionDTO);
        Task DeletePermissionAsync(int permissionId);
        Task<IEnumerable<FilePermissionDTO>> GetPermissionsByFileIdAsync(int fileId);
        Task<IEnumerable<FilePermissionDTO>> GetPermissionsByRoleIdAsync(int roleId);
    }
}
