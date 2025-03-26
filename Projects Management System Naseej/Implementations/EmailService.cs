using Projects_Management_System_Naseej.Repositories;
using System.Net.Mail;
using System.Net;

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
            // Retrieve SMTP configuration from the correct path
            var smtpHost = _configuration["Authorization:Smtp:Host"];
            var smtpPort = _configuration["Authorization:Smtp:Port"];
            var smtpUsername = _configuration["Authorization:Smtp:Username"];
            var smtpPassword = _configuration["Authorization:Smtp:Password"];
            var smtpFromEmail = _configuration["Authorization:Smtp:FromEmail"];

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
                    From = new MailAddress(smtpFromEmail, "Naseej Password Reset"),
                    Subject = "Your Password Reset OTP",
                    Body = $@"
                        <html>
                        <body>
                            <h2>Password Reset</h2>
                            <p>Your One-Time Password (OTP) is:</p>
                            <h3 style='color: blue;'>{otp}</h3>
                            <p>This OTP will expire in 10 minutes.</p>
                            <p>If you did not request this password reset, please ignore this email.</p>
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
            // Comprehensive error logging
            _logger.LogError(ex, $"Error sending OTP to {email}");

            // Additional detailed logging
            _logger.LogError($"Exception Type: {ex.GetType().Name}");
            _logger.LogError($"Exception Message: {ex.Message}");

            // Log inner exception if exists
            if (ex.InnerException != null)
            {
                _logger.LogError($"Inner Exception Type: {ex.InnerException.GetType().Name}");
                _logger.LogError($"Inner Exception Message: {ex.InnerException.Message}");
            }

            return false;
        }
    }

    private void LogSmtpConfiguration()
    {
        try
        {
            _logger.LogInformation("SMTP Configuration Details:");
            _logger.LogInformation($"Host: {_configuration["Authorization:Smtp:Host"]}");
            _logger.LogInformation($"Port: {_configuration["Authorization:Smtp:Port"]}");
            _logger.LogInformation($"From Email: {_configuration["Authorization:Smtp:FromEmail"]}");

            // Mask sensitive information
            var username = _configuration["Authorization:Smtp:Username"];
            _logger.LogInformation($"Username: {(string.IsNullOrWhiteSpace(username) ? "NOT SET" : username.Substring(0, 2) + "***")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging SMTP configuration");
        }
    }
}