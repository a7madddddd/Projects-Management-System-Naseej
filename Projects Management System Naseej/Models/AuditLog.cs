using System;
using System.Collections.Generic;

namespace Projects_Management_System_Naseej.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int UserId { get; set; }

    public string ActionType { get; set; } = null!;

    public int? FileId { get; set; }

    public DateTime? ActionDate { get; set; }

    public string? Ipaddress { get; set; }

    public string? Details { get; set; }

    public virtual File? File { get; set; }

    public virtual User User { get; set; } = null!;
}
