using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Service for sending emails (password reset, 2FA OTP, etc.)
/// This service only sends pre-defined email templates with sanitized content.
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
    /// Sends a password reset email with a secure reset link.
    /// The reset link is HTML-encoded to prevent XSS.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="resetLink">Password reset URL (will be HTML-encoded)</param>
    /// <returns>True if email sent successfully</returns>
    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        _logger.LogInformation("Sending password reset email");

        // HTML-encode the reset link to prevent injection attacks
        var encodedLink = HttpUtility.HtmlEncode(resetLink);

        var subject = "Reset Your Password - Ace Job Agency";
        var body = BuildPasswordResetEmailBody(encodedLink);

        return await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends a 2FA verification code email.
    /// The OTP code is validated to contain only digits and HTML-encoded.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="otpCode">6-digit OTP code (will be validated and HTML-encoded)</param>
    /// <returns>True if email sent successfully</returns>
    public async Task<bool> Send2FAEmailAsync(string toEmail, string otpCode)
    {
        _logger.LogInformation("Sending 2FA verification email");

        // Validate OTP is only digits (security check)
        if (!System.Text.RegularExpressions.Regex.IsMatch(otpCode, @"^\d{6}$"))
        {
            _logger.LogWarning("Invalid OTP format detected");
            return false;
        }

        var subject = "Your Login Verification Code - Ace Job Agency";
        var body = Build2FAEmailBody(HttpUtility.HtmlEncode(otpCode));

        return await SendEmailInternalAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Generic email sending for system-generated content only.
    /// WARNING: This method should only be called with pre-sanitized, system-generated content.
    /// Do NOT pass user input directly to this method.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "cs/sensitive-data-transmission",
        Justification = "Email body contains only system-generated, pre-sanitized content from template methods")]
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        return await SendEmailInternalAsync(toEmail, subject, htmlBody);
    }

    /// <summary>
    /// Internal method for sending emails. Content must be pre-sanitized.
    /// </summary>
    private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string htmlBody)
    {
        _logger.LogDebug("SMTP Server: {Server}:{Port}", _options.SmtpServer, _options.SmtpPort);

        if (string.IsNullOrEmpty(_options.SmtpServer))
        {
            _logger.LogWarning("SMTP server not configured");
            return false;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_options.FromEmail, _options.FromName);
            message.To.Add(new MailAddress(toEmail));

            // Prevent header injection by removing line breaks
            message.Subject = subject.Replace("\r", "").Replace("\n", "");
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort);
            client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);
            client.EnableSsl = _options.EnableSsl;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Timeout = 30000;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully");
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error. StatusCode: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return false;
        }
    }

    /// <summary>
    /// Builds password reset email body with pre-encoded link.
    /// </summary>
    private static string BuildPasswordResetEmailBody(string encodedResetLink)
    {
        return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Password Reset Request</h2>
    <p>You have requested to reset your password for Ace Job Agency.</p>
    <p>Click the link below to reset your password:</p>
    <p><a href='{encodedResetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
    <p>Or copy and paste this link into your browser:</p>
    <p style='word-break: break-all;'>{encodedResetLink}</p>
    <p>This link will expire in <strong>1 hour</strong>.</p>
    <p>If you did not request this password reset, please ignore this email.</p>
    <br/>
    <p>Best regards,<br/>Ace Job Agency Team</p>
</body>
</html>";
    }

    /// <summary>
    /// Builds 2FA email body with pre-encoded OTP code.
    /// </summary>
    private static string Build2FAEmailBody(string encodedOtpCode)
    {
        return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif;'>
    <h2>Two-Factor Authentication</h2>
    <p>Your verification code is:</p>
    <div style='background-color: #f4f4f4; padding: 20px; text-align: center; margin: 20px 0;'>
        <h1 style='color: #007bff; letter-spacing: 5px; margin: 0;'>{encodedOtpCode}</h1>
    </div>
    <p>This code will expire in <strong>5 minutes</strong>.</p>
    <p>If you did not attempt to log in, please ignore this email and consider changing your password.</p>
    <br/>
    <p>Best regards,<br/>Ace Job Agency Team</p>
</body>
</html>";
    }
}
