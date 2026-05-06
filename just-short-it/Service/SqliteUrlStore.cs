using JustShortIt.Model;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class SqliteUrlStore
{
    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int MaxIdLength = 16;

    private readonly JustShortItDbContext _dbContext;

    /// <summary>
    /// Creates a URL store backed by the provided EF Core context.
    /// </summary>
    /// <param name="dbContext">Database context used for redirect queries and updates.</param>
    public SqliteUrlStore(JustShortItDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Resolves an active redirect target for the specified ID.
    /// </summary>
    /// <param name="id">Short ID to resolve.</param>
    /// <returns>
    /// The target URL when the ID exists and has not expired; otherwise <see langword="null"/>.
    /// </returns>
    public async Task<string?> GetTargetAsync(string id)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await _dbContext.Redirects
            .AsNoTracking()
            .Where(x => x.Id == id && x.ExpiresAtUtc > now)
            .Select(x => x.Target)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Determines whether the specified ID currently maps to an unexpired redirect.
    /// </summary>
    /// <param name="id">Short ID to check.</param>
    /// <returns><see langword="true"/> when an active redirect exists; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ExistsAsync(string id)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await _dbContext.Redirects
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.ExpiresAtUtc > now);
    }

    /// <summary>
    /// Creates or refreshes a redirect mapping for an ID.
    /// </summary>
    /// <param name="id">Short ID to create or replace.</param>
    /// <param name="target">Destination URL for the redirect.</param>
    /// <param name="expirationUtc">Expiration timestamp expected to be in UTC.</param>
    /// <returns>
    /// <see langword="true"/> when the mapping is written; <see langword="false"/> when the ID is already held by
    /// another active redirect or a concurrent write wins the race.
    /// </returns>
    /// <remarks>
    /// If the ID exists but is expired, the existing row is updated in place rather than creating a second row.
    /// </remarks>
    public async Task<bool> CreateAsync(string id, string target, DateTime expirationUtc)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiration = new DateTimeOffset(expirationUtc).ToUnixTimeSeconds();

        var existingRedirect = await _dbContext.Redirects.SingleOrDefaultAsync(x => x.Id == id);
        if (existingRedirect is not null)
        {
            if (existingRedirect.ExpiresAtUtc > now)
            {
                return false;
            }

            existingRedirect.Target = target;
            existingRedirect.ExpiresAtUtc = expiration;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        _dbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = id,
            Target = target,
            ExpiresAtUtc = expiration
        });

        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a redirect mapping if it exists.
    /// </summary>
    /// <param name="id">Short ID to delete.</param>
    /// <remarks>
    /// This operation is idempotent; missing IDs are treated as a no-op.
    /// </remarks>
    public async Task DeleteAsync(string id)
    {
        var existingRedirect = await _dbContext.Redirects.SingleOrDefaultAsync(x => x.Id == id);
        if (existingRedirect is null)
        {
            return;
        }

        _dbContext.Redirects.Remove(existingRedirect);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Generates a short ID that is currently unused by active redirects.
    /// </summary>
    /// <returns>A unique ID composed of characters from the internal alphabet.</returns>
    /// <remarks>
    /// IDs are generated with increasing length. The generator prefers the shortest available length and ignores
    /// expired IDs when checking uniqueness.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no ID can be generated up to the configured max length.</exception>
    public async Task<string> GenerateNewId()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (var length = 1; length <= MaxIdLength; length++)
        {
            var currentLength = length;

            var existingIds = await _dbContext.Redirects
                .AsNoTracking()
                .Where(x => x.ExpiresAtUtc > now && x.Id.Length == currentLength)
                .Select(x => x.Id)
                .ToHashSetAsync();

            // If a length is fully saturated, move on to the next one.
            if (existingIds.Count >= Math.Pow(IdAlphabet.Length, currentLength))
            {
                continue;
            }

            while (true)
            {
                var candidate = GenerateCandidate(currentLength);

                if (!existingIds.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new InvalidOperationException("Unable to generate a unique ID.");
    }

    /// <summary>
    /// Generates a random candidate ID for the requested length.
    /// </summary>
    /// <param name="length">Number of characters to include in the candidate ID.</param>
    /// <returns>A random string drawn from the allowed ID alphabet.</returns>
    private static string GenerateCandidate(int length)
    {
        var chars = new char[length];

        for (var index = 0; index < length; index++)
        {
            chars[index] = IdAlphabet[Random.Shared.Next(IdAlphabet.Length)];
        }

        return new string(chars);
    }
}