using JustShortIt.Model;
using JustShortIt.Model.Database;
using JustShortIt.Service;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustShortIt.Tests;

public class SqliteUrlStoreClickTrackingTests
{
    private static readonly SqliteOptions DefaultSqliteOptions = new();

    [Test]
    public async Task CreateAsync_OnNewRedirect_SetsCreatedAtUtc()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var store = CreateStore(fixture.DbContext);

        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var created = await store.CreateAsync("abc", "https://example.test", DateTime.UtcNow.AddMinutes(30));

        await Assert.That(created).IsTrue();

        var redirect = await fixture.DbContext.Redirects.AsNoTracking().SingleAsync(x => x.Id == "abc");
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await Assert.That(redirect.CreatedAtUtc).IsGreaterThanOrEqualTo(before);
        await Assert.That(redirect.CreatedAtUtc).IsLessThanOrEqualTo(after);
    }

    [Test]
    public async Task CreateAsync_WhenRefreshingExpiredRedirect_PreservesCreatedAtUtc()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var oldCreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-3).ToUnixTimeSeconds();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        fixture.DbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = "abc",
            Target = "https://old.example",
            ExpiresAtUtc = now - 100,
            CreatedAtUtc = oldCreatedAtUtc
        });

        await fixture.DbContext.SaveChangesAsync();

        var store = CreateStore(fixture.DbContext);
        var created = await store.CreateAsync("abc", "https://new.example", DateTime.UtcNow.AddHours(2));

        await Assert.That(created).IsTrue();

        var redirect = await fixture.DbContext.Redirects.AsNoTracking().SingleAsync(x => x.Id == "abc");
        await Assert.That(redirect.CreatedAtUtc).IsEqualTo(oldCreatedAtUtc);
        await Assert.That(redirect.Target).IsEqualTo("https://new.example");
    }

    [Test]
    public async Task LogRedirectClickAsync_StoresReferrerAndSupportsNull()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var store = CreateStore(fixture.DbContext);

        var created = await store.CreateAsync("abc", "https://example.test", DateTime.UtcNow.AddHours(1));
        await Assert.That(created).IsTrue();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await store.LogRedirectClickAsync("abc", "https://ref.example", now - 1);
        await store.LogRedirectClickAsync("abc", null, now);

        var events = await store.GetClickEventsAsync("abc");

        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0].Referrer).IsNull();
        await Assert.That(events[1].Referrer).IsEqualTo("https://ref.example");
    }

    [Test]
    public async Task GetClickCountsForActiveRedirectsAsync_ExcludesExpiredRedirects()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var store = CreateStore(fixture.DbContext);

        fixture.DbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = "active",
            Target = "https://active.example",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()
        });

        fixture.DbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = "expired",
            Target = "https://expired.example",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds()
        });

        await fixture.DbContext.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        fixture.DbContext.RedirectClickEvents.AddRange(
            new RedirectClickEvent { RedirectId = "active", ClickedAtUtc = now - 3, Referrer = null },
            new RedirectClickEvent { RedirectId = "active", ClickedAtUtc = now - 2, Referrer = null },
            new RedirectClickEvent { RedirectId = "expired", ClickedAtUtc = now - 1, Referrer = null });
        await fixture.DbContext.SaveChangesAsync();

        var counts = await store.GetClickCountsForActiveRedirectsAsync();

        await Assert.That(counts.Count).IsEqualTo(1);
        await Assert.That(counts["active"]).IsEqualTo(2);
    }

    [Test]
    public async Task DeleteAsync_RemovesRedirectAndClickEvents()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var store = CreateStore(fixture.DbContext);

        var created = await store.CreateAsync("abc", "https://example.test", DateTime.UtcNow.AddHours(1));
        await Assert.That(created).IsTrue();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await store.LogRedirectClickAsync("abc", null, now);
        await store.LogRedirectClickAsync("abc", "https://ref.example", now + 1);

        await store.DeleteAsync("abc");

        await Assert.That(await fixture.DbContext.Redirects.AsNoTracking().AnyAsync(x => x.Id == "abc")).IsFalse();
        await Assert.That(await fixture.DbContext.RedirectClickEvents.AsNoTracking().AnyAsync(x => x.RedirectId == "abc")).IsFalse();
    }

    [Test]
    public async Task RemoveExpiredRedirectsAsync_RemovesExpiredRedirectsAndClickEvents()
    {
        await using var fixture = await SqliteFixture.CreateAsync();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        fixture.DbContext.Redirects.AddRange(
            new StoredUrlRedirect
            {
                Id = "active",
                Target = "https://active.example",
                ExpiresAtUtc = now + 600,
                CreatedAtUtc = now - 1200
            },
            new StoredUrlRedirect
            {
                Id = "expired",
                Target = "https://expired.example",
                ExpiresAtUtc = now - 600,
                CreatedAtUtc = now - 7200
            });
        await fixture.DbContext.SaveChangesAsync();

        fixture.DbContext.RedirectClickEvents.AddRange(
            new RedirectClickEvent { RedirectId = "active", ClickedAtUtc = now - 50, Referrer = null },
            new RedirectClickEvent { RedirectId = "expired", ClickedAtUtc = now - 40, Referrer = null });
        await fixture.DbContext.SaveChangesAsync();

        var repository = new SqliteMaintenanceRepository(fixture.DbContext, NullLogger<SqliteMaintenanceRepository>.Instance);
        await repository.RemoveExpiredRedirectsAsync();

        await Assert.That(await fixture.DbContext.Redirects.AsNoTracking().AnyAsync(x => x.Id == "active")).IsTrue();
        await Assert.That(await fixture.DbContext.Redirects.AsNoTracking().AnyAsync(x => x.Id == "expired")).IsFalse();
        await Assert.That(await fixture.DbContext.RedirectClickEvents.AsNoTracking().AnyAsync(x => x.RedirectId == "active")).IsTrue();
        await Assert.That(await fixture.DbContext.RedirectClickEvents.AsNoTracking().AnyAsync(x => x.RedirectId == "expired")).IsFalse();
    }

    private static SqliteUrlStore CreateStore(JustShortItDbContext dbContext)
    {
        return new SqliteUrlStore(
            dbContext,
            DefaultSqliteOptions,
            new TestReservedIdProvider([]),
            NullLogger<SqliteUrlStore>.Instance);
    }

    private sealed class TestReservedIdProvider(IEnumerable<string> reservedIds) : IReservedIdProvider
    {
        public IReadOnlySet<string> ReservedIds { get; } = new HashSet<string>(reservedIds, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private SqliteFixture(SqliteConnection connection, JustShortItDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
        }

        private SqliteConnection Connection { get; }
        public JustShortItDbContext DbContext { get; }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<JustShortItDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new JustShortItDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new SqliteFixture(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
