using System.ComponentModel.DataAnnotations;

namespace AppSec_Assignment_2.Models;

/// <summary>
/// Audit log entity for tracking user actions
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    public int? MemberId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    public Member? Member { get; set; }
}
