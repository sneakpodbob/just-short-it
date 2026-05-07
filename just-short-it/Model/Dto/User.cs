using System.ComponentModel.DataAnnotations;

namespace JustShortIt.Model.Dto;

/// <summary>
/// Credential model used for login binding and configuration-backed authentication.
/// </summary>
/// <param name="Username">Account username.</param>
/// <param name="Password">Account password in plain text as submitted by the login form.</param>
/// [BindProperties]
public record User([Required]string Username, [Required]string Password);