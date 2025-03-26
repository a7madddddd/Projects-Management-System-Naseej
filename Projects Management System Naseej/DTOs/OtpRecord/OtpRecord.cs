namespace Projects_Management_System_Naseej.DTOs.OtpRecord
{
    public class OtpRecord
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }

    }
}
