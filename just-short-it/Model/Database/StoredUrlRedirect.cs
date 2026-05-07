using System.ComponentModel.DataAnnotations;

namespace JustShortIt.Model.Database;

/// <summary>
/// Persistence model for redirects stored in SQLite.
/// </summary>
/// <remarks>
/// This type represents database state and is intentionally separate from UI-bound models.
/// </remarks>
public class StoredUrlRedirect
{
    [StringLength(16)]
    [Key]
    public required string Id { get; init; }

    [StringLength(3072)]
    [Required]
    public required string Target { get; set; }

    [Required]
    public required long ExpiresAtUtc { get; set; }
}