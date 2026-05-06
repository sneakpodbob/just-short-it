using JustShortIt.Model;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class SqliteUrlStore
{
    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int MaxIdLength = 16;

    private readonly JustShortItDbContext _dbContext;

    public SqliteUrlStore(JustShortItDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetTargetAsync(string id)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await _dbContext.Redirects
            .AsNoTracking()
            .Where(x => x.Id == id && x.ExpiresAtUtc > now)
            .Select(x => x.Target)
            .SingleOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(string id)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await _dbContext.Redirects
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.ExpiresAtUtc > now);
    }

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