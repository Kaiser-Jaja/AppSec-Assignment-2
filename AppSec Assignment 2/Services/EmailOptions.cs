namespace AppSec_Assignment_2.Services;

/// <summary>
/// Configuration options for email service
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
  public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Ace Job Agency";
    public bool EnableSsl { get; set; } = true;
}
