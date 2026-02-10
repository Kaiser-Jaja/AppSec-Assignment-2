using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Models;
using AppSec_Assignment_2.Services;
using AppSec_Assignment_2.ViewModels;

namespace AppSec_Assignment_2.Pages.Account;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Member> _passwordHasher;
    private readonly ReCaptchaService _reCaptchaService;
    private readonly AuditService _auditService;
    private readonly TwoFactorService _twoFactorService;
    private readonly GoogleReCaptchaOptions _reCaptchaOptions;
    private readonly ILogger<LoginModel> _logger;

    // Lockout settings
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    // Maximum password age (e.g., 90 days)
    private static readonly TimeSpan MaxPasswordAge = TimeSpan.FromDays(90);

    // OTP expiry time
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(5);

    public LoginModel(
        ApplicationDbContext context,
        IPasswordHasher<Member> passwordHasher,
        ReCaptchaService reCaptchaService,
        AuditService auditService,
        TwoFactorService twoFactorService,
        IOptions<GoogleReCaptchaOptions> reCaptchaOptions,
        ILogger<LoginModel> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _reCaptchaService = reCaptchaService;
        _auditService = auditService;
        _twoFactorService = twoFactorService;
        _reCaptchaOptions = reCaptchaOptions.Value;
        _logger = logger;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Message { get; set; }

    public string ReCaptchaSiteKey => _reCaptchaOptions.SiteKey;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

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

        if (member == null)
        {
            // Don't reveal that user doesn't exist
            await _auditService.LogAsync(null, "Failed Login", ipAddress, userAgent, $"Unknown email: {Input.Email}");
            ModelState.AddModelError(string.Empty, "Invalid email or password");
            return Page();
        }

        // Check if account is locked
        if (member.LockoutEnd.HasValue && member.LockoutEnd.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (member.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            await _auditService.LogAsync(member.Id, "Locked Login Attempt", ipAddress, userAgent);
            ModelState.AddModelError(string.Empty, $"Account is locked. Please try again in {Math.Ceiling(remainingMinutes)} minutes.");
            return Page();
        }

        // Verify password
        var result = _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, Input.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            // Increment failed attempts
            member.FailedLoginAttempts++;

            if (member.FailedLoginAttempts >= MaxFailedAttempts)
            {
                member.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                await _auditService.LogAsync(member.Id, "Account Locked", ipAddress, userAgent,
                   $"Locked after {MaxFailedAttempts} failed attempts");
                _logger.LogWarning("Account locked for member {Email} after {Attempts} failed attempts",
                  member.Email, MaxFailedAttempts);
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(member.Id, "Failed Login", ipAddress, userAgent, "Invalid password");

            ModelState.AddModelError(string.Empty, "Invalid email or password");
            return Page();
        }

        // Successful password verification - reset failed attempts
        member.FailedLoginAttempts = 0;
        member.LockoutEnd = null;
        await _context.SaveChangesAsync();

        // Check if password has expired (max password age)
        if (member.LastPasswordChangeAt.HasValue)
        {
            var passwordAge = DateTime.UtcNow - member.LastPasswordChangeAt.Value;
            if (passwordAge > MaxPasswordAge)
            {
                await _auditService.LogAsync(member.Id, "Password Expired", ipAddress, userAgent);
                return RedirectToPage("/Account/ChangePassword", new { expired = true });
            }
        }

        // Check if 2FA is enabled
        if (member.TwoFactorEnabled)
        {
            // Generate and send OTP via email
            var otpCode = _twoFactorService.GenerateOtpCode();
            member.TwoFactorOtpCode = otpCode;
            member.TwoFactorOtpExpiry = DateTime.UtcNow.Add(OtpExpiry);
            await _context.SaveChangesAsync();

            // Send OTP email
            var emailSent = await _twoFactorService.SendOtpEmailAsync(member.Email, otpCode);

            if (!emailSent)
            {
                _logger.LogError("Failed to send 2FA OTP email to {Email}", member.Email);
                ModelState.AddModelError(string.Empty, "Failed to send verification code. Please try again.");
                return Page();
            }

            // Store member info in TempData for 2FA verification
            TempData["2FA_MemberId"] = member.Id;
            TempData["2FA_Email"] = member.Email;
            TempData["2FA_Name"] = $"{member.FirstName} {member.LastName}";

            await _auditService.LogAsync(member.Id, "2FA OTP Sent", ipAddress, userAgent);
            _logger.LogInformation("2FA OTP sent to {Email}", member.Email);

            return RedirectToPage("/Account/Verify2FA", new { rememberMe = Input.RememberMe });
        }

        // No 2FA - complete login
        return await CompleteLoginAsync(member, ipAddress, userAgent);
    }

    private async Task<IActionResult> CompleteLoginAsync(Member member, string? ipAddress, string? userAgent)
    {
        // Generate new session ID for single-session enforcement
        var sessionId = Guid.NewGuid().ToString();
        member.CurrentSessionId = sessionId;

        await _context.SaveChangesAsync();

        // Create claims
        var claims = new List<Claim>
      {
            new(ClaimTypes.NameIdentifier, member.Id.ToString()),
  new(ClaimTypes.Name, $"{member.FirstName} {member.LastName}"),
 new(ClaimTypes.Email, member.Email),
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

        await _auditService.LogAsync(member.Id, "Login", ipAddress, userAgent, "Successful login");
      _logger.LogInformation("Member {Email} logged in", member.Email);

  return RedirectToPage("/Index");
    }
}
