using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace JustShortIt.Model; 

[BindProperties]
/// <summary>
/// Credential model used for login binding and configuration-backed authentication.
/// </summary>
/// <param name="Username">Account username.</param>
/// <param name="Password">Account password in plain text as submitted by the login form.</param>
public record User([Required]string Username, [Required]string Password);