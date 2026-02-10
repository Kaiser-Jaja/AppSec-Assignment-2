using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Services;
using AppSec_Assignment_2.ViewModels;

namespace AppSec_Assignment_2.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly ReCaptchaService _reCaptchaService;
    private readonly AuditService _auditService;
    private readonly GoogleReCaptchaOptions _reCaptchaOptions;
    private readonly ILogger<ForgotPasswordModel> _logger;

    private static readonly TimeSpan TokenExpiry = TimeSpan.FromHours(1);

    public ForgotPasswordModel(
     ApplicationDbContext context,
        EmailService emailService,
        ReCaptchaService reCaptchaService,
  AuditService auditService,
     IOptions<GoogleReCaptchaOptions> reCaptchaOptions,
  ILogger<ForgotPasswordModel> logger)
 {
        _context = context;
 _emailService = emailService;
        _reCaptchaService = reCaptchaService;
 _auditService = auditService;
        _reCaptchaOptions = reCaptchaOptions.Value;
        _logger = logger;
    }

    [BindProperty]
    public ForgotPasswordViewModel Input { get; set; } = new();

 public bool EmailSent { get; set; } = false;

    public string ReCaptchaSiteKey => _reCaptchaOptions.SiteKey;

    public void OnGet()
  {
    }

    public async Task<IActionResult> OnPostAsync()
{
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
      var userAgent = Request.Headers.UserAgent.ToString();

        // Log without exposing email address
_logger.LogInformation("Forgot password request received");

      // Verify reCAPTCHA if configured
        if (!string.IsNullOrEmpty(_reCaptchaOptions.SecretKey))
     {
            var captchaValid = await _reCaptchaService.VerifyTokenAsync(Input.RecaptchaToken ?? string.Empty);
       if (!captchaValid)
  {
    ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
             return Page();
     }
 }

        if (!ModelState.IsValid)
        {
      return Page();
  }

        // Find member by email
        var member = await _context.Members
   .FirstOrDefaultAsync(m => m.Email == Input.Email);

        // Always show success message to prevent email enumeration
  if (member != null)
        {
      _logger.LogInformation("Member found, generating reset token");
     
          // Generate reset token
   var token = GenerateSecureToken();
          member.PasswordResetToken = token;
      member.PasswordResetTokenExpiry = DateTime.UtcNow.Add(TokenExpiry);

      await _context.SaveChangesAsync();
    _logger.LogInformation("Reset token saved to database for member ID: {MemberId}", member.Id);

   // Generate reset link
        var resetLink = Url.Page(
                "/Account/ResetPassword",
         pageHandler: null,
             values: new { token = token, email = member.Email },
    protocol: Request.Scheme);

            // Log without exposing the reset link (contains token)
     _logger.LogInformation("Password reset link generated");

   // Send email
     var emailSent = await _emailService.SendPasswordResetEmailAsync(member.Email, resetLink!);
  
if (emailSent)
          {
   _logger.LogInformation("Password reset email sent successfully");
        }
          else
  {
          _logger.LogWarning("Failed to send password reset email");
      }

            await _auditService.LogAsync(member.Id, "Password Reset Requested", ipAddress, userAgent);
        }
    else
        {
   _logger.LogInformation("Forgot password requested for non-existent account");
         // Log attempt with unknown email (don't log the actual email)
   await _auditService.LogAsync(null, "Password Reset Requested", ipAddress, userAgent, "Unknown email attempted");
        }

      EmailSent = true;
    return Page();
    }

    private static string GenerateSecureToken()
 {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
     return Convert.ToBase64String(bytes)
    .Replace("+", "-")
      .Replace("/", "_")
     .TrimEnd('=');
    }
}
