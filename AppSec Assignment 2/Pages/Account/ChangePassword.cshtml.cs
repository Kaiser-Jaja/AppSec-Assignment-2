using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Models;
using AppSec_Assignment_2.Services;
using AppSec_Assignment_2.ViewModels;

namespace AppSec_Assignment_2.Pages.Account;

[Authorize]
[ValidateAntiForgeryToken]
public class ChangePasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Member> _passwordHasher;
private readonly AuditService _auditService;
    private readonly ILogger<ChangePasswordModel> _logger;

    // Minimum time between password changes (e.g., 1 minute for testing, could be 24 hours in production)
    private static readonly TimeSpan MinPasswordAge = TimeSpan.FromMinutes(1);
    
    // Maximum password age before requiring change (e.g., 90 days)
  private static readonly TimeSpan MaxPasswordAge = TimeSpan.FromDays(90);

    public ChangePasswordModel(
      ApplicationDbContext context,
        IPasswordHasher<Member> passwordHasher,
        AuditService auditService,
        ILogger<ChangePasswordModel> logger)
{
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
        _logger = logger;
  }

    [BindProperty]
    public ChangePasswordViewModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
var userAgent = Request.Headers.UserAgent.ToString();

        if (!ModelState.IsValid)
        {
   return Page();
        }

        var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(memberIdClaim, out int memberId))
        {
            return RedirectToPage("/Account/Login");
      }

        var member = await _context.Members.FindAsync(memberId);
        if (member == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Verify current password
        var verifyResult = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, Input.CurrentPassword);
      if (verifyResult == PasswordVerificationResult.Failed)
     {
            ModelState.AddModelError("Input.CurrentPassword", "Current password is incorrect");
         await _auditService.LogAsync(memberId, "Failed Password Change", ipAddress, userAgent, "Incorrect current password");
        return Page();
        }

        // Check minimum password age
   if (member.LastPasswordChangeAt.HasValue)
        {
    var timeSinceLastChange = DateTime.UtcNow - member.LastPasswordChangeAt.Value;
     if (timeSinceLastChange < MinPasswordAge)
 {
     var remainingTime = MinPasswordAge - timeSinceLastChange;
          ModelState.AddModelError(string.Empty, 
  $"You cannot change your password yet. Please wait {Math.Ceiling(remainingTime.TotalMinutes)} more minute(s).");
 return Page();
   }
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

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(memberId, "Password Changed", ipAddress, userAgent);

  _logger.LogInformation("Member {MemberId} changed their password", memberId);

    SuccessMessage = "Your password has been changed successfully.";
     ModelState.Clear();
        Input = new ChangePasswordViewModel();

        return Page();
    }
}
