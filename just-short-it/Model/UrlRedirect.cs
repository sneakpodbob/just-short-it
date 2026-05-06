using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace JustShortIt.Model; 

/// <summary>
/// UI model used when creating or displaying a short URL mapping.
/// </summary>
/// <param name="Id">Short identifier for the redirect. Must be between 1 and 16 characters.</param>
/// <param name="Target">Absolute destination URL.</param>
/// <param name="ExpirationDate">
/// Expiration payload serialized as <see cref="long"/> from <see cref="DateTime.ToBinary()"/>.
/// </param>
[BindProperties]
public record UrlRedirect([Required, MinLength(1), MaxLength(16)]string Id, [Required, Url]string Target, [Required]string ExpirationDate);