using Coravel.Invocable;

namespace JustShortIt.Service;

public class ExpiredRedirectCleanupInvocable : IInvocable
{
    private readonly SqliteMaintenanceRepository _repository;
    private readonly ILogger<ExpiredRedirectCleanupInvocable> _logger;

    public ExpiredRedirectCleanupInvocable(
        SqliteMaintenanceRepository repository,
        ILogger<ExpiredRedirectCleanupInvocable> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Running scheduled expired redirect cleanup job.");
        await _repository.RemoveExpiredRedirectsAsync();
    }
}
