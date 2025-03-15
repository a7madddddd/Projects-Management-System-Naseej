using System;
using System.Collections.Generic;

namespace Projects_Management_System_Naseej.Models;

public partial class File
{
    public int FileId { get; set; }

    public string FileName { get; set; } = null!;

    public string? FileExtension { get; set; }

    public string FilePath { get; set; } = null!;

    public long FileSize { get; set; }

    public int? CategoryId { get; set; }

    public int UploadedBy { get; set; }

    public DateTime? UploadDate { get; set; }

    public int? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsPublic { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual FileCategory? Category { get; set; }

    public virtual ICollection<FilePermission> FilePermissions { get; set; } = new List<FilePermission>();

    public virtual User? LastModifiedByNavigation { get; set; }

    public virtual User UploadedByNavigation { get; set; } = null!;
}
