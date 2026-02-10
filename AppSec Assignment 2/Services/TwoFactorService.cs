using System.Security.Cryptography;
using System.Web;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Service for Two-Factor Authentication using Email OTP
/// </summary>
public class TwoFactorService
{
    private readonly EmailService _emailService;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(EmailService emailService, ILogger<TwoFactorService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a 6-digit OTP code
    /// </summary>
    public string GenerateOtpCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
        return code.ToString("D6"); // Pad with zeros to ensure 6 digits
    }

    /// <summary>
    /// Sends OTP code via email
    /// </summary>
    public async Task<bool> SendOtpEmailAsync(string email, string otpCode)
    {
        // Log without exposing email address
        _logger.LogInformation("Sending 2FA OTP");

        // Sanitize OTP code (should only be digits, but ensure no injection)
        var sanitizedOtp = HttpUtility.HtmlEncode(otpCode);

        var subject = "Your Login Verification Code - Ace Job Agency";
        var body = $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <h2>Two-Factor Authentication</h2>
            <p>Your verification code is:</p>
        <div style='background-color: #f4f4f4; padding: 20px; text-align: center; margin: 20px 0;'>
      <h1 style='color: #007bff; letter-spacing: 5px; margin: 0;'>{sanitizedOtp}</h1>
  </div>
            <p>This code will expire in <strong>5 minutes</strong>.</p>
    <p>If you did not attempt to log in, please ignore this email and consider changing your password.</p>
    <br/>
       <p>Best regards,<br/>Ace Job Agency Team</p>
        </body>
    </html>";

        return await _emailService.SendEmailAsync(email, subject, body);
    }

    /// <summary>
    /// Validates if the provided code matches the expected code
    /// </summary>
    public bool ValidateCode(string expectedCode, string providedCode)
    {
        if (string.IsNullOrEmpty(expectedCode) || string.IsNullOrEmpty(providedCode))
            return false;

        return string.Equals(expectedCode.Trim(), providedCode.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
