using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Models;
using AppSec_Assignment_2.Services;
using AppSec_Assignment_2.ViewModels;

namespace AppSec_Assignment_2.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Member> _passwordHasher;
    private readonly AuditService _auditService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        ApplicationDbContext context,
        IPasswordHasher<Member> passwordHasher,
      AuditService auditService,
        ILogger<ResetPasswordModel> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
        _logger = logger;
    }

    [BindProperty]
    public ResetPasswordViewModel Input { get; set; } = new();

    public bool InvalidToken { get; set; } = false;
    public bool ResetComplete { get; set; } = false;

    public async Task<IActionResult> OnGetAsync(string? token, string? email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
      {
        InvalidToken = true;
      return Page();
        }

        // Validate token
        var member = await _context.Members
    .FirstOrDefaultAsync(m => m.Email == email &&
      m.PasswordResetToken == token &&
         m.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (member == null)
        {
    InvalidToken = true;
            return Page();
 }

        Input.Token = token;
     Input.Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
   var userAgent = Request.Headers.UserAgent.ToString();

 if (!ModelState.IsValid)
        {
 return Page();
        }

   // Validate token again
      var member = await _context.Members
        .FirstOrDefaultAsync(m => m.Email == Input.Email &&
      m.PasswordResetToken == Input.Token &&
           m.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (member == null)
        {
      InvalidToken = true;
  return Page();
        }

        // Server-side password strength validation
        if (!PasswordHelper.IsStrongPassword(Input.NewPassword))
        {
      var requirements = PasswordHelper.GetPasswordRequirements(Input.NewPassword);
      foreach (var req in requirements)
   {
    ModelState.AddModelError("Input.NewPassword", req);
          }
            return Page();
   }

    // Check password history (cannot reuse last 2 passwords)
        if (!string.IsNullOrEmpty(member.PreviousPasswordHash1))
        {
            var prev1Result = _passwordHasher.VerifyHashedPassword(member, member.PreviousPasswordHash1, Input.NewPassword);
 if (prev1Result != PasswordVerificationResult.Failed)
        {
      ModelState.AddModelError("Input.NewPassword", "Cannot reuse your previous passwords");
                return Page();
            }
        }

     if (!string.IsNullOrEmpty(member.PreviousPasswordHash2))
 {
     var prev2Result = _passwordHasher.VerifyHashedPassword(member, member.PreviousPasswordHash2, Input.NewPassword);
if (prev2Result != PasswordVerificationResult.Failed)
          {
 ModelState.AddModelError("Input.NewPassword", "Cannot reuse your previous passwords");
  return Page();
 }
    }

    // Check current password is not being reused
        var currentResult = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, Input.NewPassword);
        if (currentResult != PasswordVerificationResult.Failed)
   {
            ModelState.AddModelError("Input.NewPassword", "New password must be different from current password");
          return Page();
        }

        // Update password history
        member.PreviousPasswordHash2 = member.PreviousPasswordHash1;
        member.PreviousPasswordHash1 = member.PasswordHash;

     // Hash and save new password
   member.PasswordHash = _passwordHasher.HashPassword(member, Input.NewPassword);
        member.LastPasswordChangeAt = DateTime.UtcNow;

   // Clear reset token
        member.PasswordResetToken = null;
        member.PasswordResetTokenExpiry = null;

   // Clear any lockout
 member.FailedLoginAttempts = 0;
     member.LockoutEnd = null;

    await _context.SaveChangesAsync();
      await _auditService.LogAsync(member.Id, "Password Reset", ipAddress, userAgent);

        _logger.LogInformation("Password reset completed for {Email}", member.Email);

     ResetComplete = true;
     return Page();
    }
}
