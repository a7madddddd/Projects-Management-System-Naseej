namespace Projects_Management_System_Naseej.Repositories
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetOtpAsync(string email, string otp);

    }
}
