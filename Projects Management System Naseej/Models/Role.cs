using System;
using System.Collections.Generic;

namespace Projects_Management_System_Naseej.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<FilePermission> FilePermissions { get; set; } = new List<FilePermission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
