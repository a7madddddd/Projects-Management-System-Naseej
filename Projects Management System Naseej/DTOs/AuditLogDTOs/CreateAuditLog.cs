namespace Projects_Management_System_Naseej.DTOs.AuditLogDTOs
{
    public class CreateAuditLog
    {
        public int UserId { get; set; }
        public string ActionType { get; set; }
        public int? FileId { get; set; }
        public string Ipaddress { get; set; }
        public string Details { get; set; }
    }
}
