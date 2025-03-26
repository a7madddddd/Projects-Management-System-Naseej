using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Projects_Management_System_Naseej.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace Projects_Management_System_Naseej.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetOtpAsync(string email, string otp)
        {
            try
            {
                // Detailed logging of configuration
                LogSmtpConfiguration();

                // Retrieve SMTP configuration
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPort = _configuration["Smtp:Port"];
                var smtpUsername = _configuration["Smtp:Username"];
                var smtpPassword = _configuration["Smtp:Password"];
                var smtpFromEmail = _configuration["Smtp:FromEmail"];

                // Validate configuration
                if (string.IsNullOrWhiteSpace(smtpHost))
                {
                    _logger.LogError("SMTP Host is not configured");
                    return false;
                }

                if (!int.TryParse(smtpPort, out int port))
                {
                    _logger.LogError($"Invalid SMTP Port: {smtpPort}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(smtpUsername))
                {
                    _logger.LogError("SMTP Username is not configured");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(smtpPassword))
                {
                    _logger.LogError("SMTP Password is not configured");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(smtpFromEmail))
                {
                    _logger.LogError("SMTP From Email is not configured");
                    return false;
                }

                // Create SMTP client
                using (var smtpClient = new SmtpClient(smtpHost)
                {
                    Port = port,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                })
                {
                    // Prepare email message
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpFromEmail, "Password Reset"),
                        Subject = "Your Password Reset OTP",
                        Body = $@"
                            <html>
                            <body>
                                <h2>Password Reset</h2>
                                <p>Your One-Time Password (OTP) is:</p>
                                <h3>{otp}</h3>
                                <p>This OTP will expire in 10 minutes.</p>
                            </body>
                            </html>",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    // Send email
                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"OTP sent successfully to {email}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log the full exception details
                _logger.LogError(ex, $"Comprehensive error sending OTP to {email}");

                // Log specific exception details
                _logger.LogError($"Exception Type: {ex.GetType().Name}");
                _logger.LogError($"Exception Message: {ex.Message}");

                // If it's an authentication error, provide more context
                if (ex is SmtpException smtpEx)
                {
                    _logger.LogError($"SMTP Status Code: {smtpEx.StatusCode}");
                }

                return false;
            }
        }

        private void LogSmtpConfiguration()
        {
            // Log configuration details (be careful not to log sensitive information in production)
            _logger.LogInformation("SMTP Configuration:");
            _logger.LogInformation($"Host: {_configuration["Smtp:Host"]}");
            _logger.LogInformation($"Port: {_configuration["Smtp:Port"]}");
            _logger.LogInformation($"From Email: {_configuration["Smtp:FromEmail"]}");

            // Mask the username and password in logs
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            _logger.LogInformation($"Username: {(string.IsNullOrWhiteSpace(username) ? "NOT SET" : username.Substring(0, 2) + "***")}");
            _logger.LogInformation($"Password: {(string.IsNullOrWhiteSpace(password) ? "NOT SET" : "***")}");
        }
    }
}