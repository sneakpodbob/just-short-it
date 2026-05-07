using System.ComponentModel.DataAnnotations;

namespace JustShortIt.Model.Dto;

/// <summary>
/// Credential model used for login form binding.
/// </summary>
/// <param name="Username">Account username.</param>
/// <param name="Password">Account password as submitted by the login form.</param>
/// [BindProperties]
public record User([Required]string Username, [Required]string Password);