namespace JustShortIt.Model;

/// <summary>
/// Persistence model for redirect IDs that remain blocked after natural expiry.
/// </summary>
public class BlockedRedirectId
{
    public required string Id { get; set; }
    public long ExpiresAtUtc { get; set; }
}