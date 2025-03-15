using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.FilePermissionDTOs;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IFilePermissionRepository _filePermissionRepository;

        public PermissionsController(IFilePermissionRepository filePermissionRepository)
        {
            _filePermissionRepository = filePermissionRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FilePermissionDTO>>> GetAllPermissions()
        {
            try
            {
                var permissions = await _filePermissionRepository.GetAllPermissionsAsync();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving file permissions.");
            }
        }

        [HttpGet("{permissionId}")]
        public async Task<ActionResult<FilePermissionDTO>> GetPermissionById(int permissionId)
        {
            try
            {
                var permission = await _filePermissionRepository.GetPermissionByIdAsync(permissionId);
                if (permission == null)
                {
                    return NotFound($"File permission with ID {permissionId} not found.");
                }
                return Ok(permission);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving the file permission.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<FilePermissionDTO>> CreatePermission(CreateFilePermission permissionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdPermission = await _filePermissionRepository.CreatePermissionAsync(permissionDTO);
                return CreatedAtAction(nameof(GetPermissionById), new { permissionId = createdPermission.PermissionId }, createdPermission);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the file permission.");
            }
        }

        [HttpPut("{permissionId}")]
        public async Task<ActionResult<FilePermissionDTO>> UpdatePermission(int permissionId, UpdateFilePermission permissionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedPermission = await _filePermissionRepository.UpdatePermissionAsync(permissionId, permissionDTO);
                if (updatedPermission == null)
                {
                    return NotFound($"File permission with ID {permissionId} not found.");
                }
                return Ok(updatedPermission);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the file permission.");
            }
        }

        [HttpDelete("{permissionId}")]
        public async Task<IActionResult> DeletePermission(int permissionId)
        {
            try
            {
                await _filePermissionRepository.DeletePermissionAsync(permissionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while deleting the file permission.");
            }
        }

        [HttpGet("file/{fileId}")]
        public async Task<ActionResult<IEnumerable<FilePermissionDTO>>> GetPermissionsByFileId(int fileId)
        {
            try
            {
                var permissions = await _filePermissionRepository.GetPermissionsByFileIdAsync(fileId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving file permissions by file ID.");
            }
        }

        [HttpGet("role/{roleId}")]
        public async Task<ActionResult<IEnumerable<FilePermissionDTO>>> GetPermissionsByRoleId(int roleId)
        {
            try
            {
                var permissions = await _filePermissionRepository.GetPermissionsByRoleIdAsync(roleId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving file permissions by role ID.");
            }
        }
    }
}
