using Microsoft.EntityFrameworkCore;
using Projects_Management_System_Naseej.DTOs.AuditLogDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Implementations
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly MyDbContex _context;

        public AuditLogRepository(MyDbContex context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditLogDTO>> GetAllAuditLogsAsync()
        {
            return await _context.AuditLogs
                .Select(al => new AuditLogDTO
                {
                    LogId = al.LogId,
                    UserId = al.UserId,
                    ActionType = al.ActionType,
                    FileId = al.FileId,
                    ActionDate = al.ActionDate.GetValueOrDefault(),
                    Ipaddress = al.Ipaddress,
                    Details = al.Details
                })
                .ToListAsync();
        }

        public async Task<AuditLogDTO> GetAuditLogByIdAsync(int logId)
        {
            var log = await _context.AuditLogs.FindAsync(logId);
            if (log == null) return null;

            return new AuditLogDTO
            {
                LogId = log.LogId,
                UserId = log.UserId,
                ActionType = log.ActionType,
                FileId = log.FileId,
                ActionDate = log.ActionDate.GetValueOrDefault(),
                Ipaddress = log.Ipaddress,
                Details = log.Details
            };
        }

        public async Task<AuditLogDTO> CreateAuditLogAsync(CreateAuditLog logDTO)
        {
            var log = new AuditLog
            {
                UserId = logDTO.UserId,
                ActionType = logDTO.ActionType,
                FileId = logDTO.FileId,
                ActionDate = DateTime.Now,
                Ipaddress = logDTO.Ipaddress,
                Details = logDTO.Details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return new AuditLogDTO
            {
                LogId = log.LogId,
                UserId = log.UserId,
                ActionType = log.ActionType,
                FileId = log.FileId,
                ActionDate = log.ActionDate.GetValueOrDefault(),
                Ipaddress = log.Ipaddress,
                Details = log.Details
            };
        }

        public async Task<IEnumerable<AuditLogDTO>> GetAuditLogsByUserIdAsync(int userId)
        {
            return await _context.AuditLogs
                .Where(al => al.UserId == userId)
                .Select(al => new AuditLogDTO
                {
                    LogId = al.LogId,
                    UserId = al.UserId,
                    ActionType = al.ActionType,
                    FileId = al.FileId,
                    ActionDate = al.ActionDate.GetValueOrDefault(),
                    Ipaddress = al.Ipaddress,
                    Details = al.Details
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLogDTO>> GetAuditLogsByFileIdAsync(int fileId)
        {
            return await _context.AuditLogs
                .Where(al => al.FileId == fileId)
                .Select(al => new AuditLogDTO
                {
                    LogId = al.LogId,
                    UserId = al.UserId,
                    ActionType = al.ActionType,
                    FileId = al.FileId,
                    ActionDate = al.ActionDate.GetValueOrDefault(),
                    Ipaddress = al.Ipaddress,
                    Details = al.Details
                })
                .ToListAsync();
        }
    }
}
