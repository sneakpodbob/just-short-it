using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JustShortIt.Model.Database;

/// <summary>
/// Persistence model for a single redirect click event.
/// </summary>
public class RedirectClickEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; init; }

    [StringLength(16)]
    [Required]
    public required string RedirectId { get; init; }

    [Required]
    public required long ClickedAtUtc { get; init; }

    [StringLength(3072)]
    public string? Referrer { get; init; }

    public StoredUrlRedirect Redirect { get; init; } = null!;
}
