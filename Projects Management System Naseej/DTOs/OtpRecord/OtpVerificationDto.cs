using System.ComponentModel.DataAnnotations;

namespace Projects_Management_System_Naseej.DTOs.OtpRecord
{
    public class OtpVerificationDto
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}
