using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Services;

namespace AppSec_Assignment_2.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly ApplicationDbContext _context;
 private readonly AuditService _auditService;
    private readonly ILogger<LogoutModel> _logger;

  public LogoutModel(
      ApplicationDbContext context,
     AuditService auditService,
  ILogger<LogoutModel> logger)
    {
        _context = context;
        _auditService = auditService;
      _logger = logger;
 }

    public async Task<IActionResult> OnGetAsync()
    {
      return await LogoutAsync();
    }

  public async Task<IActionResult> OnPostAsync()
    {
     return await LogoutAsync();
    }

    private async Task<IActionResult> LogoutAsync()
    {
var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
      var userAgent = Request.Headers.UserAgent.ToString();

        if (User.Identity?.IsAuthenticated == true)
        {
        var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   
     if (int.TryParse(memberIdClaim, out int memberId))
{
        // Clear session ID in database
      var member = await _context.Members.FindAsync(memberId);
   if (member != null)
       {
            member.CurrentSessionId = null;
await _context.SaveChangesAsync();
   }

   await _auditService.LogAsync(memberId, "Logout", ipAddress, userAgent);
    _logger.LogInformation("Member {MemberId} logged out", memberId);
            }
        }

     await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
     return RedirectToPage("/Account/Login", new { message = "logged_out" });
    }
}
