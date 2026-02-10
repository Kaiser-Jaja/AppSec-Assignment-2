using System.ComponentModel.DataAnnotations;

namespace AppSec_Assignment_2.ViewModels;

/// <summary>
/// View model for 2FA setup
/// </summary>
public class Setup2FAViewModel
{
    public string SecretKey { get; set; } = string.Empty;
    
    public string QrCodeUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    [Display(Name = "Verification Code")]
    public string VerificationCode { get; set; } = string.Empty;
}

/// <summary>
/// View model for 2FA verification during login
/// </summary>
public class Verify2FAViewModel
{
    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    [Display(Name = "Verification Code")]
    public string VerificationCode { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
