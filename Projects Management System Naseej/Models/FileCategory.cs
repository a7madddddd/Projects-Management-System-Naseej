using System;
using System.Collections.Generic;

namespace Projects_Management_System_Naseej.Models;

public partial class FileCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
