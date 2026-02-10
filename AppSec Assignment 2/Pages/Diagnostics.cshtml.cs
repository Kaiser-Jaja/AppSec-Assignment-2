using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AppSec_Assignment_2.Services;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace AppSec_Assignment_2.Pages;

public class DiagnosticsModel : PageModel
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<DiagnosticsModel> _logger;

    public DiagnosticsModel(IOptions<EmailOptions> emailOptions, ILogger<DiagnosticsModel> logger)
    {
   _emailOptions = emailOptions.Value;
  _logger = logger;
  }

    public string SmtpServer => _emailOptions.SmtpServer ?? "NOT SET";
 public int SmtpPort => _emailOptions.SmtpPort;
 public string SmtpUsername => _emailOptions.SmtpUsername ?? "NOT SET";
    public string FromEmail => _emailOptions.FromEmail ?? "NOT SET";
    public bool EnableSsl => _emailOptions.EnableSsl;
    public bool PasswordSet => !string.IsNullOrEmpty(_emailOptions.SmtpPassword);
    public int PasswordLength => _emailOptions.SmtpPassword?.Length ?? 0;
    
    public string TestEmail { get; set; } = "";
    public string? TestResult { get; set; }
    public bool TestSuccess { get; set; }

  public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string testEmail)
    {
        TestEmail = testEmail;
     
        var logs = new StringBuilder();
  logs.AppendLine("=== EMAIL DIAGNOSTIC TEST ===");
  logs.AppendLine($"Timestamp: {DateTime.Now}");
  logs.AppendLine($"SMTP Server: {_emailOptions.SmtpServer}");
      logs.AppendLine($"SMTP Port: {_emailOptions.SmtpPort}");
logs.AppendLine($"SSL: {_emailOptions.EnableSsl}");
   logs.AppendLine();

        // Log without exposing sensitive information
        _logger.LogInformation("Email diagnostic test started");

  if (string.IsNullOrEmpty(_emailOptions.SmtpServer))
        {
     TestResult = "ERROR: SMTP Server is not configured in appsettings.json";
      TestSuccess = false;
   return Page();
     }

        try
   {
        logs.AppendLine("Creating email message...");
    
     using var message = new MailMessage();
    message.From = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName);
message.To.Add(new MailAddress(testEmail));
message.Subject = $"Test Email from Ace Job Agency - {DateTime.Now:HH:mm:ss}";
      message.Body = $@"<html>
<body style='font-family: Arial, sans-serif;'>
<h2>Email Test Successful!</h2>
<p>This email was sent at {DateTime.Now}</p>
<p><strong>Configuration used:</strong></p>
<ul>
<li>Server: {_emailOptions.SmtpServer}</li>
<li>Port: {_emailOptions.SmtpPort}</li>
<li>SSL: {_emailOptions.EnableSsl}</li>
</ul>
<p>If you received this email, your email configuration is working correctly!</p>
</body>
</html>";
            message.IsBodyHtml = true;

     logs.AppendLine("Creating SMTP client...");
   
        using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort);
            client.Credentials = new NetworkCredential(_emailOptions.SmtpUsername, _emailOptions.SmtpPassword);
          client.EnableSsl = _emailOptions.EnableSsl;
         client.DeliveryMethod = SmtpDeliveryMethod.Network;
     client.Timeout = 30000;

          logs.AppendLine("Sending email...");
            // Log without exposing email address
   _logger.LogInformation("Attempting to send test email");
   
            await client.SendMailAsync(message);
 
          logs.AppendLine("EMAIL SENT SUCCESSFULLY!");
     _logger.LogInformation("Test email sent successfully");

       TestResult = $@"{logs}

Email sent successfully!

Check your inbox (and spam folder) for the test email.

Configuration verified:
Server: {_emailOptions.SmtpServer}
Port: {_emailOptions.SmtpPort}
SSL: {_emailOptions.EnableSsl}";
       TestSuccess = true;
        }
        catch (SmtpException ex)
  {
            _logger.LogError(ex, "SMTP error during diagnostic test");
 logs.AppendLine($"SMTP ERROR: {ex.Message}");
 logs.AppendLine($"Status Code: {ex.StatusCode}");
          logs.AppendLine($"Inner Exception: {ex.InnerException?.Message}");
        
      TestResult = $@"{logs}

SMTP Error: {ex.Message}
Status Code: {ex.StatusCode}

Common Solutions:
- Authentication failed: Your App Password might be incorrect
  Go to https://myaccount.google.com/apppasswords
  Generate a NEW App Password
  
- Connection timeout: Firewall might be blocking port {_emailOptions.SmtpPort}
  Try a different network (mobile hotspot)
  Temporarily disable antivirus/firewall
  
- Server requires secure connection: Verify SSL is enabled";
TestSuccess = false;
      }
        catch (Exception ex)
     {
   _logger.LogError(ex, "Error during email diagnostic test");
  logs.AppendLine($"ERROR: {ex.Message}");
      
   TestResult = $@"{logs}

Error: {ex.Message}";
            TestSuccess = false;
       }

        return Page();
    }
}
