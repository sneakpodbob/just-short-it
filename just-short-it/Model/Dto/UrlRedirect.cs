using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace JustShortIt.Model.Dto;

/// <summary>
/// UI model used when creating or displaying a short URL mapping.
/// </summary>
/// <param name="Id">Short identifier for the redirect. Allowed: letters, digits, dot, underscore, dash (1-16 chars).</param>
/// <param name="Target">Absolute destination URL.</param>
/// <param name="ExpirationDate">
/// Expiration payload serialized as <see cref="long"/> from <see cref="DateTime.ToBinary()"/>.
/// </param>
[BindProperties]
public record UrlRedirect(
	[Required, MinLength(1), MaxLength(16), RegularExpression("^[A-Za-z0-9._-]{1,16}$", ErrorMessage = "ID can only contain letters, numbers, dot, underscore and dash.")]
	string Id,
	[Required, Url] string Target,
	[Required] string ExpirationDate);