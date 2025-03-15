using Projects_Management_System_Naseej.DTOs.AuditLogDTOs;
using Projects_Management_System_Naseej.Models;

namespace Projects_Management_System_Naseej.Repositories
{
    public interface IAuditLogRepository
    {
        Task<IEnumerable<AuditLogDTO>> GetAllAuditLogsAsync();
        Task<AuditLogDTO> GetAuditLogByIdAsync(int logId);
        Task<AuditLogDTO> CreateAuditLogAsync(CreateAuditLog logDTO);
        Task<IEnumerable<AuditLogDTO>> GetAuditLogsByUserIdAsync(int userId);
        Task<IEnumerable<AuditLogDTO>> GetAuditLogsByFileIdAsync(int fileId);
    }
}
