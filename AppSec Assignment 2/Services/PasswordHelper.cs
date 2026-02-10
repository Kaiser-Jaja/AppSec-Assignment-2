using System.Text.RegularExpressions;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Helper class for password validation
/// </summary>
public static class PasswordHelper
{
    /// <summary>
    /// Validates password strength
    /// Requirements:
  /// - Minimum 12 characters
    /// - At least one lowercase letter
    /// - At least one uppercase letter
    /// - At least one digit
    /// - At least one special character
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>True if password meets all requirements, false otherwise</returns>
  public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

     // Minimum 12 characters
        if (password.Length < 12)
       return false;

        // At least one lowercase letter
   if (!Regex.IsMatch(password, @"[a-z]"))
         return false;

 // At least one uppercase letter
        if (!Regex.IsMatch(password, @"[A-Z]"))
        return false;

 // At least one digit
        if (!Regex.IsMatch(password, @"\d"))
          return false;

        // At least one special character
  if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]"))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the password strength level
    /// </summary>
    /// <param name="password">The password to evaluate</param>
    /// <returns>Strength level: Weak, Medium, or Strong</returns>
    public static string GetPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
return "Weak";

        int score = 0;

        // Length checks
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // Character type checks
   if (Regex.IsMatch(password, @"[a-z]")) score++;
     if (Regex.IsMatch(password, @"[A-Z]")) score++;
        if (Regex.IsMatch(password, @"\d")) score++;
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]")) score++;

  return score switch
   {
        <= 2 => "Weak",
            <= 4 => "Medium",
      _ => "Strong"
        };
    }

    /// <summary>
    /// Gets detailed password requirements message
    /// </summary>
    /// <param name="password">The password to check</param>
    /// <returns>List of unmet requirements</returns>
    public static List<string> GetPasswordRequirements(string password)
    {
        var requirements = new List<string>();

        if (string.IsNullOrEmpty(password) || password.Length < 12)
       requirements.Add("Password must be at least 12 characters long");

        if (string.IsNullOrEmpty(password) || !Regex.IsMatch(password, @"[a-z]"))
            requirements.Add("Password must contain at least one lowercase letter");

        if (string.IsNullOrEmpty(password) || !Regex.IsMatch(password, @"[A-Z]"))
     requirements.Add("Password must contain at least one uppercase letter");

        if (string.IsNullOrEmpty(password) || !Regex.IsMatch(password, @"\d"))
     requirements.Add("Password must contain at least one digit");

        if (string.IsNullOrEmpty(password) || !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]"))
      requirements.Add("Password must contain at least one special character");

     return requirements;
    }
}
