using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace AppSec_Assignment_2.Services;

/// <summary>
/// Service for encrypting/decrypting sensitive member data like NRIC
/// Uses ASP.NET Core Data Protection API
/// </summary>
public class MemberProtectionService
{
    private readonly IDataProtector _protector;

 public MemberProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("MemberNRIC");
    }

 /// <summary>
    /// Encrypts the NRIC value
    /// </summary>
    /// <param name="nric">Plain text NRIC</param>
    /// <returns>Encrypted NRIC string</returns>
    public string ProtectNric(string nric)
    {
        if (string.IsNullOrEmpty(nric))
     return string.Empty;

        return _protector.Protect(nric);
    }

    /// <summary>
    /// Decrypts the NRIC value
    /// </summary>
    /// <param name="protectedNric">Encrypted NRIC string</param>
    /// <returns>Plain text NRIC, or null if decryption fails</returns>
    public string? UnprotectNric(string protectedNric)
    {
     if (string.IsNullOrEmpty(protectedNric))
      return null;

        try
      {
            return _protector.Unprotect(protectedNric);
        }
        catch (CryptographicException)
      {
   // Decryption failed - key may have changed or data is corrupted
     return null;
        }
    }
}
