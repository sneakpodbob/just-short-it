using System.ComponentModel.DataAnnotations;

namespace JustShortIt.Model;

/// <summary>
/// Persistence model for redirect IDs that remain blocked after natural expiry.
/// </summary>
public class BlockedRedirectId
{
    [StringLength(16)]
    public required string Id { get; init; }
    public long ExpiresAtUtc { get; set; }
}