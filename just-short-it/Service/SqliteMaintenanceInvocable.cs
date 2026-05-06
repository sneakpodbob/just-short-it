using Coravel.Invocable;

namespace JustShortIt.Service;

public class SqliteMaintenanceInvocable : IInvocable
{
    private readonly SqliteMaintenanceRepository _repository;
    private readonly ILogger<SqliteMaintenanceInvocable> _logger;

    public SqliteMaintenanceInvocable(
        SqliteMaintenanceRepository repository,
        ILogger<SqliteMaintenanceInvocable> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Running scheduled SQLite maintenance job.");
        await _repository.CompactAndCompressDatabaseAsync();
    }
}
