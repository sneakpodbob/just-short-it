using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class SqliteMaintenanceRepository
{
    private readonly JustShortItDbContext _dbContext;

    public SqliteMaintenanceRepository(JustShortItDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RemoveExpiredRedirectsAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _dbContext.Redirects
            .Where(x => x.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync();
    }

    public async Task CompactAndCompressDatabaseAsync()
    {
        // VACUUM rebuilds the DB file to reclaim free pages and compact storage.
        await _dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
    }
}
