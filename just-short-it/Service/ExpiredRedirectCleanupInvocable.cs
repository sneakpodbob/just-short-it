using Coravel.Invocable;

namespace JustShortIt.Service;

public class ExpiredRedirectCleanupInvocable : IInvocable
{
    private readonly SqliteMaintenanceRepository _repository;

    public ExpiredRedirectCleanupInvocable(SqliteMaintenanceRepository repository)
    {
        _repository = repository;
    }

    public async Task Invoke()
    {
        await _repository.RemoveExpiredRedirectsAsync();
    }
}
