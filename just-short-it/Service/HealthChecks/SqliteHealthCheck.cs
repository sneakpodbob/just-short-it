using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustShortIt.Service.HealthChecks;

/// <summary>
/// Verifies that the SQLite database is reachable and readable.
/// Uses a scoped <see cref="JustShortItDbContext"/> per check execution to avoid
/// holding a connection open between probes.
/// </summary>
public sealed class SqliteHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JustShortItDbContext>();

            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
                return HealthCheckResult.Unhealthy("Cannot connect to the SQLite database.");

            var redirectCount = await db.Redirects.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy(data: new Dictionary<string, object>
            {
                ["redirect_count"] = redirectCount
            });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite health check threw an exception.", ex);
        }
    }
}
