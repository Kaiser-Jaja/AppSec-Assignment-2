using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Services;

namespace AppSec_Assignment_2.Pages.Account;

[Authorize]
public class Setup2FAModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly TwoFactorService _twoFactorService;
    private readonly AuditService _auditService;
    private readonly ILogger<Setup2FAModel> _logger;

    public Setup2FAModel(
     ApplicationDbContext context,
        TwoFactorService twoFactorService,
        AuditService auditService,
   ILogger<Setup2FAModel> logger)
    {
  _context = context;
        _twoFactorService = twoFactorService;
 _auditService = auditService;
   _logger = logger;
    }

    public bool Is2FAEnabled { get; set; } = false;
    public string MemberEmail { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null)
      {
   return RedirectToPage("/Account/Login");
        }

        Is2FAEnabled = member.TwoFactorEnabled;
MemberEmail = member.Email;

        return Page();
    }

 public async Task<IActionResult> OnPostAsync()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var member = await GetCurrentMemberAsync();
        if (member == null)
   {
        return RedirectToPage("/Account/Login");
        }

      _logger.LogInformation("Attempting to enable 2FA for member {Email}", member.Email);

   // Send a test OTP to verify email is working
        var testCode = _twoFactorService.GenerateOtpCode();
        
        _logger.LogInformation("Generated test OTP code, attempting to send email...");
    
        var emailSent = await _twoFactorService.SendOtpEmailAsync(member.Email, testCode);

     if (!emailSent)
        {
            _logger.LogError("Failed to send test email to {Email}", member.Email);
  
            ErrorMessage = @"Failed to send test email. Please verify:
    
• Your email configuration in appsettings.json is correct
• SMTP Server: smtp.gmail.com
• SMTP Port: 587
• You're using a Gmail App Password (not your regular password)
• 2-Step Verification is enabled on your Gmail account

Check the application logs for more details.";
         
      ModelState.AddModelError(string.Empty, "Failed to send test email. Please check your email configuration.");
            MemberEmail = member.Email;
            return Page();
        }

        // Enable 2FA
        member.TwoFactorEnabled = true;
        member.TwoFactorSecretKey = null; // Clear old TOTP secret if any
        await _context.SaveChangesAsync();

    await _auditService.LogAsync(member.Id, "2FA Enabled", ipAddress, userAgent);
        _logger.LogInformation("2FA enabled for member {MemberId}", member.Id);

        Is2FAEnabled = true;
        MemberEmail = member.Email;

        TempData["SuccessMessage"] = "Two-factor authentication has been enabled! A test email was sent to verify your email address.";

 return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
  var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var member = await GetCurrentMemberAsync();
        if (member == null)
        {
     return RedirectToPage("/Account/Login");
        }

  member.TwoFactorEnabled = false;
        member.TwoFactorSecretKey = null;
        member.TwoFactorOtpCode = null;
        member.TwoFactorOtpExpiry = null;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(member.Id, "2FA Disabled", ipAddress, userAgent);

    _logger.LogInformation("2FA disabled for member {MemberId}", member.Id);

        return RedirectToPage("/Account/Setup2FA");
    }

    private async Task<Models.Member?> GetCurrentMemberAsync()
    {
        var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(memberIdClaim, out int memberId))
   {
      return null;
        }

 return await _context.Members.FindAsync(memberId);
    }
}
