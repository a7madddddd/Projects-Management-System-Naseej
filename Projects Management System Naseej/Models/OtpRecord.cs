using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Projects_Management_System_Naseej.Models;
[Index(nameof(Email), nameof(IsUsed), IsUnique = true, Name = "IX_OtpRecords_Email_IsUsed")]

public partial class OtpRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = null!;
    [Required]
    [MaxLength(10)]
    public string Otp { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }

}
