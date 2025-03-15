using System;
using System.Collections.Generic;

namespace Projects_Management_System_Naseej.Models;

public partial class FilePermission
{
    public int PermissionId { get; set; }

    public int FileId { get; set; }

    public int RoleId { get; set; }

    public bool? CanView { get; set; }

    public bool? CanEdit { get; set; }

    public bool? CanUpload { get; set; }

    public bool? CanDownload { get; set; }

    public bool? CanDelete { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual File File { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
