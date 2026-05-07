using System.ComponentModel.DataAnnotations;

namespace JustShortIt.Model.Dto;

/// <summary>
/// Configuration-backed account used for password-hash verification.
/// </summary>
/// <param name="Username">Account username.</param>
/// <param name="PasswordHash">BCrypt hash of the salted password.</param>
/// <param name="PasswordSalt">Additional salt concatenated to the raw password before hashing.</param>
public record ConfiguredAccount(
    [Required] string Username,
    [Required] string PasswordHash,
    [Required] string PasswordSalt);