using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Service for sending emails (password reset, 2FA OTP, etc.)
/// </summary>
public class EmailService
{
  private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
    _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends a password reset email
    /// </summary>
    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        _logger.LogInformation("Sending password reset email to {Email}", toEmail);

        var subject = "Reset Your Password - Ace Job Agency";
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Password Reset Request</h2>
    <p>You have requested to reset your password for Ace Job Agency.</p>
    <p>Click the link below to reset your password:</p>
    <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
    <p>Or copy and paste this link into your browser:</p>
    <p style='word-break: break-all;'>{resetLink}</p>
    <p>This link will expire in <strong>1 hour</strong>.</p>
    <p>If you did not request this password reset, please ignore this email.</p>
    <br/>
    <p>Best regards,<br/>Ace Job Agency Team</p>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends an email
    /// </summary>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
     _logger.LogInformation("=== EMAIL SEND ATTEMPT ===");
        _logger.LogInformation("To: {Email}", toEmail);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("SMTP Server: {Server}:{Port}", _options.SmtpServer, _options.SmtpPort);
  _logger.LogInformation("Username: {Username}", _options.SmtpUsername);
        _logger.LogInformation("EnableSsl: {SSL}", _options.EnableSsl);

 // If SMTP is not configured, log and return false
        if (string.IsNullOrEmpty(_options.SmtpServer))
        {
            _logger.LogWarning("SMTP server not configured. Email not sent to {Email}", toEmail);
            return false;
        }

        try
     {
            using var message = new MailMessage();
            message.From = new MailAddress(_options.FromEmail, _options.FromName);
            message.To.Add(new MailAddress(toEmail));
  message.Subject = subject;
            message.Body = htmlBody;
     message.IsBodyHtml = true;

   using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort);
    client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);
      
     // For Mailtrap port 2525, use EnableSsl = true (it uses STARTTLS)
          // For Mailtrap port 465, use EnableSsl = true (implicit SSL)
            // For Mailtrap port 587, use EnableSsl = true (STARTTLS)
            client.EnableSsl = _options.EnableSsl;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
client.Timeout = 30000; // 30 seconds timeout

  _logger.LogInformation("Sending email via SMTP...");
            await client.SendMailAsync(message);

      _logger.LogInformation("=== EMAIL SENT SUCCESSFULLY ===");
 return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Email}. StatusCode: {StatusCode}, Message: {Message}", 
 toEmail, ex.StatusCode, ex.Message);
      return false;
        }
        catch (Exception ex)
        {
         _logger.LogError(ex, "Error sending email to {Email}: {Message}", toEmail, ex.Message);
            return false;
        }
    }
}
