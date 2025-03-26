using Microsoft.EntityFrameworkCore;
using Projects_Management_System_Naseej.DTOs.OtpRecord;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Implementations
{
    public class OtpRepository : IOtpRepository
    {
        private readonly MyDbContext _context;
        private readonly ILogger<OtpRepository> _logger;

        public OtpRepository(MyDbContext context, ILogger<OtpRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> StoreOtpAsync(string email, string otp)
        {
            try
            {
                // Remove any existing unused OTPs for this email
                var existingOtps = _context.OtpRecords
                    .Where(o => o.Email == email && !o.IsUsed);
                _context.OtpRecords.RemoveRange(existingOtps);

                // Create new OTP record
                var otpRecord = new Models.OtpRecord
                {
                    Email = email,
                    Otp = otp,
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                _context.OtpRecords.Add(otpRecord);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error storing OTP for {email}");
                return false;
            }
        }

        public async Task<bool> ValidateOtpAsync(string email, string otp)
        {
            try
            {
                var otpRecord = await _context.OtpRecords
                    .FirstOrDefaultAsync(o =>
                        o.Email == email &&
                        o.Otp == otp &&
                        !o.IsUsed &&
                        o.CreatedAt > DateTime.UtcNow.AddMinutes(-10));

                if (otpRecord == null)
                    return false;

                // Mark OTP as used
                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating OTP for {email}");
                return false;
            }
        }
    }
}
