using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Services;
using AppSec_Assignment_2.ViewModels;

namespace AppSec_Assignment_2.Pages.Account;

public class Verify2FAModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly TwoFactorService _twoFactorService;
    private readonly AuditService _auditService;
    private readonly ILogger<Verify2FAModel> _logger;

    // OTP expiry time for resend
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(5);

    public Verify2FAModel(
      ApplicationDbContext context,
TwoFactorService twoFactorService,
        AuditService auditService,
        ILogger<Verify2FAModel> logger)
    {
        _context = context;
      _twoFactorService = twoFactorService;
        _auditService = auditService;
   _logger = logger;
    }

 [BindProperty]
    public Verify2FAViewModel Input { get; set; } = new();

    public IActionResult OnGet(bool rememberMe = false)
    {
   // Check if we have pending 2FA data in TempData
       if (TempData["2FA_MemberId"] == null)
        {
       return RedirectToPage("/Account/Login");
     }

  Input.RememberMe = rememberMe;

    // Keep TempData for POST
        TempData.Keep("2FA_MemberId");
        TempData.Keep("2FA_Email");
        TempData.Keep("2FA_Name");

    return Page();
    }

  public async Task<IActionResult> OnPostAsync()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
var userAgent = Request.Headers.UserAgent.ToString();

        // Retrieve member info from TempData
        if (TempData["2FA_MemberId"] == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var memberId = (int)TempData["2FA_MemberId"]!;
        var email = TempData["2FA_Email"]?.ToString() ?? string.Empty;
  var name = TempData["2FA_Name"]?.ToString() ?? string.Empty;

    if (!ModelState.IsValid)
      {
    // Keep TempData for retry
 TempData["2FA_MemberId"] = memberId;
 TempData["2FA_Email"] = email;
        TempData["2FA_Name"] = name;
 return Page();
        }

        var member = await _context.Members.FindAsync(memberId);
        if (member == null)
   {
   return RedirectToPage("/Account/Login");
        }

        // Check if OTP has expired
      if (!member.TwoFactorOtpExpiry.HasValue || member.TwoFactorOtpExpiry.Value < DateTime.UtcNow)
  {
       ModelState.AddModelError(string.Empty, "The verification code has expired. Please request a new one.");
         TempData["2FA_MemberId"] = memberId;
        TempData["2FA_Email"] = email;
 TempData["2FA_Name"] = name;
    await _auditService.LogAsync(memberId, "2FA Code Expired", ipAddress, userAgent);
   return Page();
        }

        // Validate the OTP code
        if (!_twoFactorService.ValidateCode(member.TwoFactorOtpCode ?? string.Empty, Input.VerificationCode))
  {
            ModelState.AddModelError("Input.VerificationCode", "Invalid verification code. Please try again.");
   TempData["2FA_MemberId"] = memberId;
        TempData["2FA_Email"] = email;
            TempData["2FA_Name"] = name;

        await _auditService.LogAsync(memberId, "Failed 2FA Verification", ipAddress, userAgent);
      return Page();
        }

      // Clear the OTP code after successful verification
        member.TwoFactorOtpCode = null;
        member.TwoFactorOtpExpiry = null;

// Generate new session ID
        var sessionId = Guid.NewGuid().ToString();
        member.CurrentSessionId = sessionId;
     await _context.SaveChangesAsync();

   // Create claims and sign in
        var claims = new List<Claim>
    {
          new(ClaimTypes.NameIdentifier, member.Id.ToString()),
        new(ClaimTypes.Name, name),
     new(ClaimTypes.Email, email),
            new("SessionId", sessionId)
  };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
  IsPersistent = Input.RememberMe,
      ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
        };

    await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
  authProperties);

        await _auditService.LogAsync(memberId, "Login (2FA)", ipAddress, userAgent, "Successful 2FA verification");
        _logger.LogInformation("Member {Email} completed 2FA login", email);

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostResendOtpAsync()
    {
 var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Retrieve member info from TempData
        if (TempData["2FA_MemberId"] == null)
        {
  return RedirectToPage("/Account/Login");
        }

        var memberId = (int)TempData["2FA_MemberId"]!;
        var email = TempData["2FA_Email"]?.ToString() ?? string.Empty;
        var name = TempData["2FA_Name"]?.ToString() ?? string.Empty;

var member = await _context.Members.FindAsync(memberId);
        if (member == null)
      {
      return RedirectToPage("/Account/Login");
        }

        // Generate new OTP
        var otpCode = _twoFactorService.GenerateOtpCode();
        member.TwoFactorOtpCode = otpCode;
      member.TwoFactorOtpExpiry = DateTime.UtcNow.Add(OtpExpiry);
        await _context.SaveChangesAsync();

        // Send OTP email
        var emailSent = await _twoFactorService.SendOtpEmailAsync(member.Email, otpCode);

 if (emailSent)
    {
       _logger.LogInformation("2FA OTP resent to {Email}", member.Email);
            await _auditService.LogAsync(memberId, "2FA OTP Resent", ipAddress, userAgent);
     TempData["SuccessMessage"] = "A new verification code has been sent to your email.";
 }
        else
        {
       _logger.LogError("Failed to resend 2FA OTP to {Email}", member.Email);
  TempData["ErrorMessage"] = "Failed to send verification code. Please try again.";
      }

     // Keep TempData for the page
        TempData["2FA_MemberId"] = memberId;
 TempData["2FA_Email"] = email;
     TempData["2FA_Name"] = name;

        return RedirectToPage("/Account/Verify2FA", new { rememberMe = Input.RememberMe });
    }
}
