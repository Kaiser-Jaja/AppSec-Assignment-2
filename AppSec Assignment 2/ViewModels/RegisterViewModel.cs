using System.ComponentModel.DataAnnotations;

namespace AppSec_Assignment_2.ViewModels;

/// <summary>
/// View model for Ace Job Agency member registration
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "NRIC is required")]
    [Display(Name = "NRIC")]
    public string Nric { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; } = DateTime.Today;

    [Display(Name = "Resume")]
    public IFormFile? Resume { get; set; }

    /// <summary>
    /// Free-form text field - allows all characters including special characters
    /// No validation attributes to allow all input
    /// </summary>
    [Display(Name = "Who Am I")]
    public string? WhoAmI { get; set; }

    /// <summary>
    /// Google reCAPTCHA v3 token
    /// </summary>
    public string? RecaptchaToken { get; set; }
}
