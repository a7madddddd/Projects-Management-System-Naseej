namespace Projects_Management_System_Naseej.Repositories
{
    public interface IOtpRepository
    {
        Task<bool> StoreOtpAsync(string email, string otp);
        Task<bool> ValidateOtpAsync(string email, string otp);
    }
}
