using System.ComponentModel.DataAnnotations;

namespace Projects_Management_System_Naseej.DTOs.OtpRecord
{
    public class ChangePasswordRequest2
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        public string ResetToken { get; set; }
    }
}
