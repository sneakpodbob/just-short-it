using Coravel.Invocable;

namespace JustShortIt.Service;

public class SqliteMaintenanceInvocable : IInvocable
{
    private readonly SqliteMaintenanceRepository _repository;

    public SqliteMaintenanceInvocable(SqliteMaintenanceRepository repository)
    {
        _repository = repository;
    }

    public async Task Invoke()
    {
        await _repository.CompactAndCompressDatabaseAsync();
    }
}
