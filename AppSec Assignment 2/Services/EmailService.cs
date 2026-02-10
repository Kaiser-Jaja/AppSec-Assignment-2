using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Web;

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
        // Log without exposing email address
        _logger.LogInformation("Sending password reset email");

        // Sanitize the reset link to prevent any injection
  var sanitizedLink = HttpUtility.HtmlEncode(resetLink);

 var subject = "Reset Your Password - Ace Job Agency";
   var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Password Reset Request</h2>
    <p>You have requested to reset your password for Ace Job Agency.</p>
    <p>Click the link below to reset your password:</p>
    <p><a href='{sanitizedLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
    <p>Or copy and paste this link into your browser:</p>
    <p style='word-break: break-all;'>{sanitizedLink}</p>
    <p>This link will expire in <strong>1 hour</strong>.</p>
    <p>If you did not request this password reset, please ignore this email.</p>
    <br/>
    <p>Best regards,<br/>Ace Job Agency Team</p>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends an email - internal use only with pre-sanitized content
    /// </summary>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
// Log without exposing sensitive information
        _logger.LogInformation("Attempting to send email");
  _logger.LogDebug("SMTP Server: {Server}:{Port}", _options.SmtpServer, _options.SmtpPort);

      // If SMTP is not configured, log and return false
        if (string.IsNullOrEmpty(_options.SmtpServer))
        {
     _logger.LogWarning("SMTP server not configured. Email not sent.");
 return false;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_options.FromEmail, _options.FromName);
    message.To.Add(new MailAddress(toEmail));
            
            // Sanitize subject to prevent header injection
  message.Subject = subject.Replace("\r", "").Replace("\n", "");
       
          // Body should already be sanitized by caller, but ensure it's properly encoded
    message.Body = htmlBody;
       message.IsBodyHtml = true;
 
         // Add security headers to email
        message.Headers.Add("X-Mailer", "Ace Job Agency");

   using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort);
        client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);

            // For Mailtrap and other services that use STARTTLS
      client.EnableSsl = _options.EnableSsl;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
         client.Timeout = 30000; // 30 seconds timeout

            await client.SendMailAsync(message);

      _logger.LogInformation("Email sent successfully");
  return true;
    }
    catch (SmtpException ex)
        {
            // Log error without exposing email address
            _logger.LogError(ex, "SMTP error sending email. StatusCode: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
  {
   // Log error without exposing email address
         _logger.LogError(ex, "Error sending email");
     return false;
        }
    }
}
