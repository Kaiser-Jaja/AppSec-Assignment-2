using System.ComponentModel.DataAnnotations;

namespace AppSec_Assignment_2.ViewModels;

/// <summary>
/// View model for member login
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

  [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// Google reCAPTCHA v3 token
    /// </summary>
    public string? RecaptchaToken { get; set; }
}
