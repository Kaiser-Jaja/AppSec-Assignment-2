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
    /// Generates a cryptographically secure 6-digit OTP code
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
    /// Sends OTP code via email using the secure email template
    /// </summary>
    public async Task<bool> SendOtpEmailAsync(string email, string otpCode)
    {
        _logger.LogInformation("Sending 2FA OTP");

        // Use the dedicated 2FA email method which validates and sanitizes the OTP
        return await _emailService.Send2FAEmailAsync(email, otpCode);
    }

    /// <summary>
    /// Validates if the provided code matches the expected code using constant-time comparison
    /// </summary>
    public bool ValidateCode(string expectedCode, string providedCode)
    {
        if (string.IsNullOrEmpty(expectedCode) || string.IsNullOrEmpty(providedCode))
            return false;

        // Trim whitespace
        expectedCode = expectedCode.Trim();
        providedCode = providedCode.Trim();

        // Constant-time comparison to prevent timing attacks
        if (expectedCode.Length != providedCode.Length)
            return false;

        var result = 0;
        for (int i = 0; i < expectedCode.Length; i++)
        {
            result |= expectedCode[i] ^ providedCode[i];
        }

        return result == 0;
    }
}
