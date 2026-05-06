namespace JustShortIt.Model;

/// <summary>
/// Persistence model for redirects stored in SQLite.
/// </summary>
/// <remarks>
/// This type represents database state and is intentionally separate from UI-bound models.
/// </remarks>
public class StoredUrlRedirect
{
    public required string Id { get; set; }
    public required string Target { get; set; }
    public long ExpiresAtUtc { get; set; }
}