namespace Projects_Management_System_Naseej.DTOs.OtpRecord
{
    public class OtpResetPasswordRequest
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
