using JustShortIt.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustShortIt.Service.HealthChecks;

/// <summary>
/// Checks available disk space on the drive that hosts the SQLite database file.
/// Returns <see cref="HealthCheckResult.Degraded"/> below 500 MB and
/// <see cref="HealthCheckResult.Unhealthy"/> below 100 MB — thresholds that matter
/// for a SQLite-backed app where disk exhaustion means no writes at all.
/// </summary>
public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private const long DegradedThresholdMb = 500;
    private const long UnhealthyThresholdMb = 100;

    private readonly SqliteOptions _sqliteOptions;

    public DiskSpaceHealthCheck(SqliteOptions sqliteOptions)
    {
        _sqliteOptions = sqliteOptions;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Resolve the database path using the same logic as Program.cs so the
        // check always targets the drive that actually holds the database file.
        var dbPath = string.IsNullOrWhiteSpace(_sqliteOptions.Path)
            ? "data/justshortit.db"
            : _sqliteOptions.Path;

        if (!Path.IsPathRooted(dbPath))
            dbPath = Path.GetFullPath(dbPath, AppContext.BaseDirectory);

        var pathRoot = Path.GetPathRoot(dbPath);
        if (string.IsNullOrEmpty(pathRoot))
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Could not determine the root of the database path."));

        DriveInfo drive;
        try
        {
            drive = new DriveInfo(pathRoot);
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"Could not read drive info for '{pathRoot}'.", ex));
        }

        if (!drive.IsReady)
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"Drive '{pathRoot}' is not ready."));

        var freeMb = drive.AvailableFreeSpace / (1024 * 1024);
        var totalMb = drive.TotalSize / (1024 * 1024);

        var data = new Dictionary<string, object>
        {
            ["free_mb"] = freeMb,
            ["total_mb"] = totalMb,
            ["drive"] = pathRoot
        };

        return freeMb switch
        {
            < UnhealthyThresholdMb => Task.FromResult(HealthCheckResult.Unhealthy(
                $"Critical disk space: {freeMb} MB free on '{pathRoot}'.", data: data)),
            < DegradedThresholdMb => Task.FromResult(HealthCheckResult.Degraded(
                $"Low disk space: {freeMb} MB free on '{pathRoot}'.", data: data)),
            _ => Task.FromResult(HealthCheckResult.Healthy(data: data))
        };
    }
}
