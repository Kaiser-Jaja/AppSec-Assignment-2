using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AppSec_Assignment_2.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppSec_Assignment_2.Middleware;

/// <summary>
/// Middleware to validate session and prevent multiple concurrent logins
/// Compares session ID claim with database to enforce single session
/// </summary>
public class SessionValidationMiddleware
{
 private readonly RequestDelegate _next;
    private readonly ILogger<SessionValidationMiddleware> _logger;

  public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
    {
      _next = next;
        _logger = logger;
    }

 public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
  {
 if (context.User.Identity?.IsAuthenticated == true)
        {
  var sessionIdClaim = context.User.FindFirst("SessionId")?.Value;
     var memberIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

   if (!string.IsNullOrEmpty(sessionIdClaim) && int.TryParse(memberIdClaim, out int memberId))
            {
       var member = await dbContext.Members
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.Id == memberId);

   if (member == null || member.CurrentSessionId != sessionIdClaim)
   {
        _logger.LogWarning("Session invalidated for member {MemberId}. Session logged in elsewhere.",
       memberId);

           // Sign out the user - their session is no longer valid
      await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/Account/Login?message=session_expired");
         return;
   }
            }
        }

     await _next(context);
    }
}

/// <summary>
/// Extension method to add session validation middleware
/// </summary>
public static class SessionValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionValidationMiddleware>();
 }
}
