using System;
using System.Collections.Generic;

namespace Projects_Management_System_Naseej.Models;

public partial class OtpRecord
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Otp { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }
}
