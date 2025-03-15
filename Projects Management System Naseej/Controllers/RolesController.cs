using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.RoleDTOs;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        public RolesController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDTO>>> GetAllRoles()
        {
            try
            {
                var roles = await _roleRepository.GetAllRolesAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving roles.");
            }
        }

        [HttpGet("{roleId}")]
        public async Task<ActionResult<RoleDTO>> GetRoleById(int roleId)
        {
            try
            {
                var role = await _roleRepository.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    return NotFound($"Role with ID {roleId} not found.");
                }
                return Ok(role);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving the role.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<RoleDTO>> CreateRole(CreateRoleDTO roleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdRole = await _roleRepository.CreateRoleAsync(roleDTO);
                return CreatedAtAction(nameof(GetRoleById), new { roleId = createdRole.RoleId }, createdRole);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the role.");
            }
        }

        [HttpPut("{roleId}")]
        public async Task<ActionResult<RoleDTO>> UpdateRole(int roleId, UpdateRoleDTO roleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedRole = await _roleRepository.UpdateRoleAsync(roleId, roleDTO);
                if (updatedRole == null)
                {
                    return NotFound($"Role with ID {roleId} not found.");
                }
                return Ok(updatedRole);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the role.");
            }
        }

        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            try
            {
                await _roleRepository.DeleteRoleAsync(roleId);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while deleting the role.");
            }
        }
    }
}
