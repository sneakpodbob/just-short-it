## Plan: Expired ID Reuse Cooldown

Add a second persistence table for blocked redirect IDs, but populate it when a redirect is created or refreshed, not only during cleanup. That keeps the two-table design you want and avoids a bug in the simpler approach: if the block row is created only during the hourly cleanup, an ID becomes reusable immediately after expiry and stays reusable until cleanup runs.

**Steps**
1. Add a new SQLite setting in [just-short-it/Model/SqliteOptions.cs](just-short-it/Model/SqliteOptions.cs) and bind it in [just-short-it/Program.cs](just-short-it/Program.cs). Recommended name: `ExpiredIdReuseBlockSeconds`. Recommended default: `5184000` seconds, which is 60 days.
2. Add a new persistence entity and DbSet in [just-short-it/Service/JustShortItDbContext.cs](just-short-it/Service/JustShortItDbContext.cs) for a table such as `blocked_redirect_ids` with only `Id` and `ExpiresAtUtc`. In this table, `ExpiresAtUtc` means “ID becomes reusable after this time.”
3. Add schema bootstrap for existing SQLite files in [just-short-it/Program.cs](just-short-it/Program.cs). The app currently uses `EnsureCreatedAsync`, which will not add a new table to an already-existing database. We should use EF migrations to manage these DB changes. The migration for the new table should be added to the project and applied at startup if pending.
4. Update [just-short-it/Service/SqliteUrlStore.cs](just-short-it/Service/SqliteUrlStore.cs) so successful redirect creation or refresh also upserts a block row with the same ID and `block.ExpiresAtUtc = redirect.ExpiresAtUtc + ExpiredIdReuseBlockSeconds`.
5. Change availability rules in [just-short-it/Service/SqliteUrlStore.cs](just-short-it/Service/SqliteUrlStore.cs):
   1. `GenerateNewId()` must exclude IDs that are either active redirects or still present in the block table.
   2. `CreateAsync()` for a user-specified ID must fail if the ID is active or blocked.
   3. `ExistsAsync()` and `GetTargetAsync()` should continue to mean “active redirect exists,” not “ID is blocked.”
6. Preserve the chosen product behavior that manual delete does not start a cooldown. Because the block row is created ahead of time, `DeleteAsync()` in [just-short-it/Service/SqliteUrlStore.cs](just-short-it/Service/SqliteUrlStore.cs) must remove both the redirect row and any matching block row.
7. Extend cleanup in [just-short-it/Service/SqliteMaintenanceRepository.cs](just-short-it/Service/SqliteMaintenanceRepository.cs) so it deletes expired redirects from `redirects` and separately deletes expired rows from `blocked_redirect_ids`. Logging should report both counts.
8. Update defaults and documentation in [just-short-it/appsettings.json](just-short-it/appsettings.json) and [README.md](README.md) to describe the new setting and lifecycle: active redirect, then expired-but-blocked, then reusable after block expiry.
9. Replace the old reuse assumption in [just-short-it.Tests/SqliteUrlStoreIdGenerationTests.cs](just-short-it.Tests/SqliteUrlStoreIdGenerationTests.cs). The current test suite explicitly asserts that expired IDs are immediately reusable, so that expectation must be inverted.

**Relevant files**
- [just-short-it/Service/SqliteUrlStore.cs](just-short-it/Service/SqliteUrlStore.cs) — create, delete, existence checks, and ID generation
- [just-short-it/Service/SqliteMaintenanceRepository.cs](just-short-it/Service/SqliteMaintenanceRepository.cs) — scheduled cleanup
- [just-short-it/Service/JustShortItDbContext.cs](just-short-it/Service/JustShortItDbContext.cs) — EF Core mapping
- [just-short-it/Program.cs](just-short-it/Program.cs) — config binding, schema bootstrap, scheduler
- [just-short-it/Model/SqliteOptions.cs](just-short-it/Model/SqliteOptions.cs) — config shape
- [just-short-it/appsettings.json](just-short-it/appsettings.json) — default config
- [README.md](README.md) — lifecycle and config docs
- [just-short-it.Tests/SqliteUrlStoreIdGenerationTests.cs](just-short-it.Tests/SqliteUrlStoreIdGenerationTests.cs) — behavior tests

**Verification**
1. Add tests proving that naturally expired IDs remain blocked until the block row expires.
2. Add tests proving that manual delete removes any block and makes the ID immediately reusable.
3. Add tests proving that ID generation treats both active redirects and blocked IDs as unavailable.
4. Run `dotnet test --solution JustShortIt.slnx -c Release`.
5. Run `dotnet build JustShortIt.slnx`.
6. Verify against an existing SQLite file that startup creates the new table without deleting the old database.

**Decisions**
- Cooldown applies only after natural expiry.
- Manual delete from Inspect should not trigger cooldown.
- Default cooldown is a fixed 60 days.
- The second table should be part of availability checks, but it must not be populated only during cleanup.