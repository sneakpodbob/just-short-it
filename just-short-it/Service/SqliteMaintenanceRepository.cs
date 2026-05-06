using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class SqliteMaintenanceRepository
{
    private readonly JustShortItDbContext _dbContext;
    private readonly ILogger<SqliteMaintenanceRepository> _logger;

    public SqliteMaintenanceRepository(JustShortItDbContext dbContext, ILogger<SqliteMaintenanceRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RemoveExpiredRedirectsAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var deletedCount = await _dbContext.Redirects
            .Where(x => x.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Expired redirect cleanup removed {DeletedCount} entries.", deletedCount);
    }

    public async Task CompactAndCompressDatabaseAsync()
    {
        // VACUUM rebuilds the DB file to reclaim free pages and compact storage.
        _logger.LogInformation("Starting SQLite VACUUM maintenance operation.");
        await _dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
        _logger.LogInformation("Completed SQLite VACUUM maintenance operation.");
    }
}
