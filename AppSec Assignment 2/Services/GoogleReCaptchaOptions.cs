namespace AppSec_Assignment_2.Services;

/// <summary>
/// Configuration options for Google reCAPTCHA v3
/// </summary>
public class GoogleReCaptchaOptions
{
    public const string SectionName = "GoogleReCaptcha";

    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public double MinimumScore { get; set; } = 0.5;
}
