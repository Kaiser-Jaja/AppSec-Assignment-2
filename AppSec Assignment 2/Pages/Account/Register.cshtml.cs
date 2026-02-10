using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Models;
using AppSec_Assignment_2.Services;
using AppSec_Assignment_2.ViewModels;

namespace AppSec_Assignment_2.Pages.Account;

public class RegisterModel : PageModel
{
 private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Member> _passwordHasher;
    private readonly MemberProtectionService _memberProtectionService;
    private readonly ReCaptchaService _reCaptchaService;
    private readonly AuditService _auditService;
    private readonly GoogleReCaptchaOptions _reCaptchaOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
    ApplicationDbContext context,
      IPasswordHasher<Member> passwordHasher,
        MemberProtectionService memberProtectionService,
        ReCaptchaService reCaptchaService,
 AuditService auditService,
   IOptions<GoogleReCaptchaOptions> reCaptchaOptions,
        IWebHostEnvironment environment,
      ILogger<RegisterModel> logger)
    {
     _context = context;
  _passwordHasher = passwordHasher;
     _memberProtectionService = memberProtectionService;
        _reCaptchaService = reCaptchaService;
    _auditService = auditService;
        _reCaptchaOptions = reCaptchaOptions.Value;
     _environment = environment;
        _logger = logger;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

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

        // Server-side password strength validation
        if (!PasswordHelper.IsStrongPassword(Input.Password))
     {
   var requirements = PasswordHelper.GetPasswordRequirements(Input.Password);
    foreach (var req in requirements)
      {
      ModelState.AddModelError("Input.Password", req);
            }
  }

      // Check if email already exists
        var existingMember = await _context.Members
   .AsNoTracking()
    .FirstOrDefaultAsync(m => m.Email == Input.Email);

        if (existingMember != null)
        {
            ModelState.AddModelError("Input.Email", "Email already registered");
        }

        // Validate resume file
 string? resumeFileName = null;
      string? resumeContentType = null;
        long? resumeFileSize = null;

      if (Input.Resume != null)
{
            var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
       const long maxFileSize = 2 * 1024 * 1024; // 2 MB

          if (!allowedTypes.Contains(Input.Resume.ContentType))
  {
              ModelState.AddModelError("Input.Resume", "Only PDF and DOCX files are allowed");
            }

            if (Input.Resume.Length > maxFileSize)
      {
    ModelState.AddModelError("Input.Resume", "File size cannot exceed 2 MB");
        }
     }

        if (!ModelState.IsValid)
   {
      return Page();
        }

        // Save resume file
        if (Input.Resume != null)
 {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "resumes");
  Directory.CreateDirectory(uploadsPath);

            var extension = Path.GetExtension(Input.Resume.FileName);
            resumeFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, resumeFileName);

 using var stream = new FileStream(filePath, FileMode.Create);
            await Input.Resume.CopyToAsync(stream);

            resumeContentType = Input.Resume.ContentType;
resumeFileSize = Input.Resume.Length;
        }

        // Create new member
        var member = new Member
        {
 FirstName = Input.FirstName,
    LastName = Input.LastName,
          Gender = Input.Gender,
            Email = Input.Email,
            DateOfBirth = Input.DateOfBirth,
     WhoAmI = Input.WhoAmI,
            ResumeFileName = resumeFileName,
            ResumeContentType = resumeContentType,
            ResumeFileSize = resumeFileSize,
            CreatedAt = DateTime.UtcNow
        };

        // Encrypt NRIC
        member.Nric = _memberProtectionService.ProtectNric(Input.Nric);

// Hash password
        member.PasswordHash = _passwordHasher.HashPassword(member, Input.Password);

        // Save to database
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(member.Id, "Registration", ipAddress, userAgent, "New member registered");

        _logger.LogInformation("New member registered: {Email}", Input.Email);

        return RedirectToPage("/Account/Login", new { message = "registered" });
    }
}
