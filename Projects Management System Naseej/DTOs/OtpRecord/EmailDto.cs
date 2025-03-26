using System.ComponentModel.DataAnnotations;

namespace Projects_Management_System_Naseej.DTOs.OtpRecord
{
    public class EmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
