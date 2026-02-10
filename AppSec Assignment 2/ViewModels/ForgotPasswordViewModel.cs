using System.ComponentModel.DataAnnotations;

namespace AppSec_Assignment_2.ViewModels;

/// <summary>
/// View model for requesting password reset
/// </summary>
public class ForgotPasswordViewModel
{
 [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public string? RecaptchaToken { get; set; }
}
