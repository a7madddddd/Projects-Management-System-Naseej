using Projects_Management_System_Naseej.Repositories;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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
            // Retrieve SMTP configuration
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPort = _configuration["Smtp:Port"];
            var smtpUsername = _configuration["Smtp:Username"];
            var smtpPassword = _configuration["Smtp:Password"];
            var smtpFromEmail = _configuration["Smtp:FromEmail"];

            // Comprehensive configuration logging
            _logger.LogInformation("SMTP Configuration Details:");
            _logger.LogInformation($"Host: {smtpHost}");
            _logger.LogInformation($"Port: {smtpPort}");
            _logger.LogInformation($"Username: {smtpUsername}");
            _logger.LogInformation($"From Email: {smtpFromEmail}");

            // Validate configuration
            if (string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(smtpPort) ||
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword) ||
                string.IsNullOrWhiteSpace(smtpFromEmail))
            {
                _logger.LogError("Incomplete SMTP configuration");
                return false;
            }

            // Create SMTP client with detailed configuration
            using (var smtpClient = new SmtpClient(smtpHost)
            {
                Port = int.Parse(smtpPort),
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000 // 20 seconds timeout
            })
            {
                // Optional: Add certificate validation callback
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;

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
                            <h3 style='color: blue; font-size: 24px;'>{otp}</h3>
                            <p>This OTP will expire in 10 minutes.</p>
                            <p>If you did not request this password reset, please ignore this email.</p>
                            <small>Sent from Naseej Password Reset System</small>
                        </body>
                        </html>",
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };
                mailMessage.To.Add(email);

                try
                {
                    // Send email with comprehensive error handling
                    _logger.LogInformation($"Attempting to send email to {email}");

                    await Task.Run(() => smtpClient.Send(mailMessage));

                    _logger.LogInformation($"Email sent successfully to {email}");
                    return true;
                }
                catch (SmtpException smtpEx)
                {
                    _logger.LogError(smtpEx, $"SMTP Error sending email to {email}");
                    _logger.LogError($"SMTP Status Code: {smtpEx.StatusCode}");
                    _logger.LogError($"SMTP Error Message: {smtpEx.Message}");

                    // Additional inner exception logging
                    if (smtpEx.InnerException != null)
                    {
                        _logger.LogError($"Inner Exception: {smtpEx.InnerException.GetType().Name}");
                        _logger.LogError($"Inner Exception Message: {smtpEx.InnerException.Message}");
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Comprehensive error sending email to {email}");

                    // Log all exception details
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Fatal error in email sending process for {email}");
            return false;
        }
    }
}