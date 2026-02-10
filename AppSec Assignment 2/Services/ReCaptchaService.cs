using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Service for verifying Google reCAPTCHA v3 tokens
/// </summary>
public class ReCaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleReCaptchaOptions _options;
    private readonly ILogger<ReCaptchaService> _logger;

    public ReCaptchaService(
      HttpClient httpClient,
        IOptions<GoogleReCaptchaOptions> options,
        ILogger<ReCaptchaService> logger)
    {
        _httpClient = httpClient;
_options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Verifies a reCAPTCHA v3 token
  /// </summary>
    /// <param name="token">The token from the client</param>
    /// <returns>True if verification succeeds with acceptable score, false otherwise</returns>
  public async Task<bool> VerifyTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("ReCAPTCHA token is empty");
  return false;
   }

      try
     {
            var parameters = new Dictionary<string, string>
      {
         { "secret", _options.SecretKey },
          { "response", token }
            };

   var content = new FormUrlEncodedContent(parameters);
   var response = await _httpClient.PostAsync(
   "https://www.google.com/recaptcha/api/siteverify",
   content);

          if (!response.IsSuccessStatusCode)
            {
         _logger.LogError("ReCAPTCHA verification request failed with status {StatusCode}",
         response.StatusCode);
        return false;
 }

          var jsonResponse = await response.Content.ReadAsStringAsync();
 var result = JsonSerializer.Deserialize<ReCaptchaResponse>(jsonResponse,
         new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (result == null)
            {
                _logger.LogError("Failed to deserialize reCAPTCHA response");
       return false;
            }

         if (!result.Success)
  {
     _logger.LogWarning("ReCAPTCHA verification failed. Errors: {Errors}",
  string.Join(", ", result.ErrorCodes ?? Array.Empty<string>()));
           return false;
       }

    if (result.Score < _options.MinimumScore)
     {
       _logger.LogWarning("ReCAPTCHA score {Score} is below minimum {MinScore}",
      result.Score, _options.MinimumScore);
      return false;
    }

 _logger.LogInformation("ReCAPTCHA verification succeeded with score {Score}", result.Score);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying reCAPTCHA token");
     return false;
  }
    }

    private class ReCaptchaResponse
    {
     public bool Success { get; set; }
     public double Score { get; set; }
        public string? Action { get; set; }
        public string? Challenge_ts { get; set; }
        public string? Hostname { get; set; }
        public string[]? ErrorCodes { get; set; }
    }
}
