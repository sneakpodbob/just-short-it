namespace JustShortIt.Model;

public class StoredUrlRedirect
{
    public required string Id { get; set; }
    public required string Target { get; set; }
    public long ExpiresAtUtc { get; set; }
}