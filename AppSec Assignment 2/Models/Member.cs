using System.ComponentModel.DataAnnotations;

namespace AppSec_Assignment_2.Models;

/// <summary>
/// Member entity for Ace Job Agency membership registration
/// </summary>
public class Member
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// NRIC stored encrypted using Data Protection API
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Nric { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password hash - never store plain password
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(255)]
    public string? ResumeFileName { get; set; }

    [MaxLength(100)]
    public string? ResumeContentType { get; set; }

    public long? ResumeFileSize { get; set; }

    /// <summary>
    /// Free-form text field - allows all characters, HTML-encoded on display
    /// </summary>
    public string? WhoAmI { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current session ID for single-session enforcement
    /// </summary>
    public string? CurrentSessionId { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Lockout end time if account is locked
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    // ===== Password History & Age =====

    /// <summary>
    /// Previous password hash (for password history - max 2)
    /// </summary>
    public string? PreviousPasswordHash1 { get; set; }

    /// <summary>
    /// Second previous password hash (for password history - max 2)
    /// </summary>
    public string? PreviousPasswordHash2 { get; set; }

    /// <summary>
    /// Last password change timestamp (for min/max password age)
    /// </summary>
    public DateTime? LastPasswordChangeAt { get; set; }

    // ===== Password Reset =====

    /// <summary>
    /// Password reset token for email-based reset
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Password reset token expiry
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // ===== 2FA Email OTP =====

    /// <summary>
    /// Whether 2FA is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Current OTP code for 2FA (temporary, cleared after use)
    /// </summary>
    public string? TwoFactorOtpCode { get; set; }

    /// <summary>
    /// OTP code expiry time
    /// </summary>
    public DateTime? TwoFactorOtpExpiry { get; set; }

    // Keep for backward compatibility but no longer used
    public string? TwoFactorSecretKey { get; set; }
}
