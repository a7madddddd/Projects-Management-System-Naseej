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
                // Log detailed validation attempt information
                _logger.LogInformation($"OTP Validation Attempt - Email: {email}, OTP: {otp}");

                // Find the most recent unused OTP for this email within the last 10 minutes
                var otpRecord = await _context.OtpRecords
                    .Where(o =>
                        o.Email == email &&
                        o.Otp == otp &&
                        !o.IsUsed &&
                        o.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    // Log additional diagnostic information
                    var existingRecords = await _context.OtpRecords
                        .Where(o => o.Email == email)
                        .ToListAsync();

                    _logger.LogWarning($"OTP Validation Failed - Existing Records Count: {existingRecords.Count}");
                    foreach (var record in existingRecords)
                    {
                        _logger.LogWarning($"Existing Record - OTP: {record.Otp}, Created: {record.CreatedAt}, IsUsed: {record.IsUsed}");
                    }

                    return false;
                }

                // Mark OTP as used
                try
                {
                    otpRecord.IsUsed = true;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Log the specific exception
                    _logger.LogError(ex, $"Error marking OTP as used for {email}");

                    // If concurrent update causes issues, we'll still consider the OTP valid
                    _logger.LogWarning($"Concurrent OTP update detected for {email}");
                }

                _logger.LogInformation($"OTP Validated Successfully for {email}");
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
