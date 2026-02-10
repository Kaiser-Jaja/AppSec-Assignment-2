using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Models;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Service for creating audit log entries
/// </summary>
public class AuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
  _context = context;
    }

    /// <summary>
    /// Logs an action to the audit log
    /// </summary>
    /// <param name="memberId">The member ID (nullable for anonymous actions)</param>
    /// <param name="action">The action being performed</param>
    /// <param name="ipAddress">Client IP address</param>
 /// <param name="userAgent">Client user agent</param>
    /// <param name="details">Additional details</param>
 public async Task LogAsync(int? memberId, string action, string? ipAddress, string? userAgent, string? details = null)
    {
        var auditLog = new AuditLog
        {
    MemberId = memberId,
            Action = action,
            IPAddress = ipAddress,
     UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
        Details = details,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
