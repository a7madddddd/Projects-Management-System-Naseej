using Microsoft.EntityFrameworkCore;
using Projects_Management_System_Naseej.DTOs.FilePermissionDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Implementations
{
    public class FilePermissionRepository : IFilePermissionRepository
    {
        private readonly MyDbContext _context;

        public FilePermissionRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FilePermissionDTO>> GetAllPermissionsAsync()
        {
            return await _context.FilePermissions
                .Select(fp => new FilePermissionDTO
                {
                    PermissionId = fp.PermissionId,
                    FileId = fp.FileId,
                    RoleId = fp.RoleId,
                    CanView = fp.CanView.GetValueOrDefault(),
                    CanEdit = fp.CanEdit.GetValueOrDefault(),
                    CanUpload = fp.CanUpload.GetValueOrDefault(),
                    CanDownload = fp.CanDownload.GetValueOrDefault(),
                    CanDelete = fp.CanDelete.GetValueOrDefault(),
                    StartDate = fp.StartDate,
                    EndDate = fp.EndDate
                })
                .ToListAsync();
        }

        public async Task<FilePermissionDTO> GetPermissionByIdAsync(int permissionId)
        {
            var permission = await _context.FilePermissions.FindAsync(permissionId);
            if (permission == null) return null;

            return new FilePermissionDTO
            {
                PermissionId = permission.PermissionId,
                FileId = permission.FileId,
                RoleId = permission.RoleId,
                CanView = permission.CanView.GetValueOrDefault(),
                CanEdit = permission.CanEdit.GetValueOrDefault(),
                CanUpload = permission.CanUpload.GetValueOrDefault(),
                CanDownload = permission.CanDownload.GetValueOrDefault(),
                CanDelete = permission.CanDelete.GetValueOrDefault(),
                StartDate = permission.StartDate,
                EndDate = permission.EndDate
            };
        }

        public async Task<FilePermissionDTO> CreatePermissionAsync(CreateFilePermission permissionDTO)
        {
            var permission = new FilePermission
            {
                FileId = permissionDTO.FileId,
                RoleId = permissionDTO.RoleId,
                CanView = permissionDTO.CanView,
                CanEdit = permissionDTO.CanEdit,
                CanUpload = permissionDTO.CanUpload,
                CanDownload = permissionDTO.CanDownload,
                CanDelete = permissionDTO.CanDelete,
                StartDate = permissionDTO.StartDate,
                EndDate = permissionDTO.EndDate
            };

            _context.FilePermissions.Add(permission);
            await _context.SaveChangesAsync();

            return new FilePermissionDTO
            {
                PermissionId = permission.PermissionId,
                FileId = permission.FileId,
                RoleId = permission.RoleId,
                CanView = permission.CanView.GetValueOrDefault(),
                CanEdit = permission.CanEdit.GetValueOrDefault(),
                CanUpload = permission.CanUpload.GetValueOrDefault(),
                CanDownload = permission.CanDownload.GetValueOrDefault(),
                CanDelete = permission.CanDelete.GetValueOrDefault(),
                StartDate = permission.StartDate,
                EndDate = permission.EndDate
            };
        }

        public async Task<FilePermissionDTO> UpdatePermissionAsync(int permissionId, UpdateFilePermission permissionDTO)
        {
            var permission = await _context.FilePermissions.FindAsync(permissionId);
            if (permission == null) return null;

            permission.CanView = permissionDTO.CanView ?? permission.CanView;
            permission.CanEdit = permissionDTO.CanEdit ?? permission.CanEdit;
            permission.CanUpload = permissionDTO.CanUpload ?? permission.CanUpload;
            permission.CanDownload = permissionDTO.CanDownload ?? permission.CanDownload;
            permission.CanDelete = permissionDTO.CanDelete ?? permission.CanDelete;
            permission.StartDate = permissionDTO.StartDate;
            permission.EndDate = permissionDTO.EndDate;

            _context.FilePermissions.Update(permission);
            await _context.SaveChangesAsync();

            return new FilePermissionDTO
            {
                PermissionId = permission.PermissionId,
                FileId = permission.FileId,
                RoleId = permission.RoleId,
                CanView = permission.CanView.GetValueOrDefault(),
                CanEdit = permission.CanEdit.GetValueOrDefault(),
                CanUpload = permission.CanUpload.GetValueOrDefault(),
                CanDownload = permission.CanDownload.GetValueOrDefault(),
                CanDelete = permission.CanDelete.GetValueOrDefault(),
                StartDate = permission.StartDate,
                EndDate = permission.EndDate
            };
        }

        public async Task DeletePermissionAsync(int permissionId)
        {
            var permission = await _context.FilePermissions.FindAsync(permissionId);
            if (permission != null)
            {
                _context.FilePermissions.Remove(permission);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<FilePermissionDTO>> GetPermissionsByFileIdAsync(int fileId)
        {
            return await _context.FilePermissions
                .Where(fp => fp.FileId == fileId)
                .Select(fp => new FilePermissionDTO
                {
                    PermissionId = fp.PermissionId,
                    FileId = fp.FileId,
                    RoleId = fp.RoleId,
                    CanView = fp.CanView.GetValueOrDefault(),
                    CanEdit = fp.CanEdit.GetValueOrDefault(),
                    CanUpload = fp.CanUpload.GetValueOrDefault(),
                    CanDownload = fp.CanDownload.GetValueOrDefault(),
                    CanDelete = fp.CanDelete.GetValueOrDefault(),
                    StartDate = fp.StartDate,
                    EndDate = fp.EndDate
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FilePermissionDTO>> GetPermissionsByRoleIdAsync(int roleId)
        {
            return await _context.FilePermissions
                .Where(fp => fp.RoleId == roleId)
                .Select(fp => new FilePermissionDTO
                {
                    PermissionId = fp.PermissionId,
                    FileId = fp.FileId,
                    RoleId = fp.RoleId,
                    CanView = fp.CanView.GetValueOrDefault(),
                    CanEdit = fp.CanEdit.GetValueOrDefault(),
                    CanUpload = fp.CanUpload.GetValueOrDefault(),
                    CanDownload = fp.CanDownload.GetValueOrDefault(),
                    CanDelete = fp.CanDelete.GetValueOrDefault(),
                    StartDate = fp.StartDate,
                    EndDate = fp.EndDate
                })
                .ToListAsync();
        }
    }
}
