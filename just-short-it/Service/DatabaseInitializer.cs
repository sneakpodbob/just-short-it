using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Serilog;
using System.Data;

namespace JustShortIt.Service;

internal static class DatabaseInitializer
{
    private const string InitialMigrationId = "20260507000100_InitialSqliteSchema";
    private const string BlockedIdsMigrationId = "20260507000200_AddBlockedRedirectIds";

    public static async Task InitializeAsync(JustShortItDbContext dbContext)
    {
        if (await NeedsLegacyBootstrapAsync(dbContext))
            await BootstrapMigrationHistoryAsync(dbContext);

        await dbContext.Database.MigrateAsync();
        Log.Information("Database migration check completed.");
    }

    // Detects a pre-migration SQLite database: the redirects table already exists but the
    // EF migrations history table has not been created yet.
    private static async Task<bool> NeedsLegacyBootstrapAsync(JustShortItDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlite())
            return false;

        var historyRepo = dbContext.GetInfrastructure().GetRequiredService<IHistoryRepository>();
        if (await historyRepo.ExistsAsync())
            return false;

        return await TableExistsAsync(dbContext, "redirects");
    }

    // Registers already-applied migrations into EF's history table without re-running them.
    // Uses EF's own IHistoryRepository to generate the DDL/DML so no SQL is hand-written here.
    private static async Task BootstrapMigrationHistoryAsync(JustShortItDbContext dbContext)
    {
        var historyRepo = dbContext.GetInfrastructure().GetRequiredService<IHistoryRepository>();
        var productVersion = dbContext.Model.FindAnnotation("ProductVersion")?.Value as string ?? "unknown";

        await dbContext.Database.ExecuteSqlRawAsync(historyRepo.GetCreateIfNotExistsScript());

        await dbContext.Database.ExecuteSqlRawAsync(
            historyRepo.GetInsertScript(new HistoryRow(InitialMigrationId, productVersion)));

        if (await TableExistsAsync(dbContext, "blocked_redirect_ids"))
            await dbContext.Database.ExecuteSqlRawAsync(
                historyRepo.GetInsertScript(new HistoryRow(BlockedIdsMigrationId, productVersion)));

        Log.Information("Bootstrapped EF migration history for legacy SQLite database.");
    }

    private static async Task<bool> TableExistsAsync(JustShortItDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return result is long and > 0;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
