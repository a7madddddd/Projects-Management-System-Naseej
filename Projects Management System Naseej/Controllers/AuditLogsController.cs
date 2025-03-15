using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.AuditLogDTOs;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogsController(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAllAuditLogs()
        {
            try
            {
                var auditLogs = await _auditLogRepository.GetAllAuditLogsAsync();
                return Ok(auditLogs);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving audit logs.");
            }
        }

        [HttpGet("{logId}")]
        public async Task<ActionResult<AuditLogDTO>> GetAuditLogById(int logId)
        {
            try
            {
                var auditLog = await _auditLogRepository.GetAuditLogByIdAsync(logId);
                if (auditLog == null)
                {
                    return NotFound($"Audit log with ID {logId} not found.");
                }
                return Ok(auditLog);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving the audit log.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<AuditLogDTO>> CreateAuditLog(CreateAuditLog auditLogDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdAuditLog = await _auditLogRepository.CreateAuditLogAsync(auditLogDTO);
                return CreatedAtAction(nameof(GetAuditLogById), new { logId = createdAuditLog.LogId }, createdAuditLog);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the audit log.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAuditLogsByUserId(int userId)
        {
            try
            {
                var auditLogs = await _auditLogRepository.GetAuditLogsByUserIdAsync(userId);
                return Ok(auditLogs);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving audit logs by user ID.");
            }
        }

        [HttpGet("file/{fileId}")]
        public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAuditLogsByFileId(int fileId)
        {
            try
            {
                var auditLogs = await _auditLogRepository.GetAuditLogsByFileIdAsync(fileId);
                return Ok(auditLogs);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving audit logs by file ID.");
            }
        }
    }
}
