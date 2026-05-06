using JustShortIt.Model;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class SqliteUrlStore
{
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
}