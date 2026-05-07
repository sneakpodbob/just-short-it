using JustShortIt.Model;
using JustShortIt.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustShortIt.Tests;

public class SqliteUrlStoreIdGenerationTests
{
    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly SqliteOptions DefaultSqliteOptions = new();
    private static readonly IReservedIdProvider EmptyReservedIdProvider = new TestReservedIdProvider(Array.Empty<string>());

    /// <summary>
    /// Verifies the generator starts with the shortest possible ID length when no active IDs exist.
    /// </summary>
    [Test]
    public async Task GenerateNewId_OnEmptyStore_ReturnsSingleCharacterId()
    {
        await using var dbContext = CreateDbContext();
        var store = CreateStore(dbContext);

        var id = await store.GenerateNewId();

        await Assert.That(id.Length).IsEqualTo(1);
        await Assert.That(IdAlphabet.Contains(id[0])).IsTrue();
    }

    /// <summary>
    /// Verifies the generator can still resolve the only free one-character ID in a nearly full keyspace.
    /// </summary>
    [Test]
    public async Task GenerateNewId_WhenAllSingleCharacterIdsButOneAreTaken_ReturnsOnlyRemainingId()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var c in IdAlphabet.Where(x => x != 'Z'))
        {
            dbContext.Redirects.Add(new StoredUrlRedirect
            {
                Id = c.ToString(),
                Target = "https://example.test",
                ExpiresAtUtc = now + 3600
            });
        }

        await dbContext.SaveChangesAsync();

        var store = CreateStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id).IsEqualTo("Z");
    }

    /// <summary>
    /// Verifies generation moves to the next length once the complete one-character keyspace is occupied.
    /// </summary>
    [Test]
    public async Task GenerateNewId_WhenSingleCharacterSpaceIsSaturated_UsesNextLength()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var c in IdAlphabet)
        {
            dbContext.Redirects.Add(new StoredUrlRedirect
            {
                Id = c.ToString(),
                Target = "https://example.test",
                ExpiresAtUtc = now + 3600
            });
        }

        await dbContext.SaveChangesAsync();

        var store = CreateStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id.Length).IsEqualTo(2);
        await Assert.That(id.All(c => IdAlphabet.Contains(c))).IsTrue();
    }

    /// <summary>
    /// Verifies expired redirects remain unavailable while their cooldown block is still active.
    /// </summary>
    [Test]
    public async Task GenerateNewId_WhenOnlyBlockedIdRemains_UsesNextLength()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var c in IdAlphabet.Where(x => x != 'Q'))
        {
            dbContext.Redirects.Add(new StoredUrlRedirect
            {
                Id = c.ToString(),
                Target = "https://example.test",
                ExpiresAtUtc = now + 3600
            });
        }

        dbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = "Q",
            Target = "https://expired.test",
            ExpiresAtUtc = now - 10
        });

        dbContext.BlockedRedirectIds.Add(new BlockedRedirectId
        {
            Id = "Q",
            ExpiresAtUtc = now + 3600
        });

        await dbContext.SaveChangesAsync();

        var store = CreateStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id.Length).IsEqualTo(2);
    }

    /// <summary>
    /// Verifies IDs become reusable again once the cooldown block has expired.
    /// </summary>
    [Test]
    public async Task GenerateNewId_WhenCooldownExpired_ReusesExpiredId()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var c in IdAlphabet.Where(x => x != 'Q'))
        {
            dbContext.Redirects.Add(new StoredUrlRedirect
            {
                Id = c.ToString(),
                Target = "https://example.test",
                ExpiresAtUtc = now + 3600
            });
        }

        dbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = "Q",
            Target = "https://expired.test",
            ExpiresAtUtc = now - 10
        });

        dbContext.BlockedRedirectIds.Add(new BlockedRedirectId
        {
            Id = "Q",
            ExpiresAtUtc = now - 1
        });

        await dbContext.SaveChangesAsync();

        var store = CreateStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id).IsEqualTo("Q");
    }

    /// <summary>
    /// Verifies create requests fail when the redirect is expired but its cooldown block is still active.
    /// </summary>
    [Test]
    public async Task CreateAsync_WhenIdIsBlockedAfterExpiry_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        dbContext.Redirects.Add(new StoredUrlRedirect
        {
            Id = "abc",
            Target = "https://expired.test",
            ExpiresAtUtc = now - 10
        });

        dbContext.BlockedRedirectIds.Add(new BlockedRedirectId
        {
            Id = "abc",
            ExpiresAtUtc = now + 3600
        });

        await dbContext.SaveChangesAsync();

        var store = CreateStore(dbContext);
        var created = await store.CreateAsync("abc", "https://new.example", DateTime.UtcNow.AddHours(1));

        await Assert.That(created).IsFalse();
    }

    /// <summary>
    /// Verifies reserved route IDs are rejected by CreateAsync.
    /// </summary>
    [Test]
    public async Task CreateAsync_WhenIdIsReserved_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var store = CreateStore(dbContext, reservedIdProvider: new TestReservedIdProvider(["Login"]));

        var created = await store.CreateAsync("Login", "https://example.test", DateTime.UtcNow.AddHours(1));

        await Assert.That(created).IsFalse();
    }

    /// <summary>
    /// Verifies reserved route IDs are blocked case-insensitively.
    /// </summary>
    [Test]
    public async Task CreateAsync_WhenIdIsReservedCaseVariant_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var store = CreateStore(dbContext, reservedIdProvider: new TestReservedIdProvider(["Login"]));

        var created = await store.CreateAsync("login", "https://example.test", DateTime.UtcNow.AddHours(1));

        await Assert.That(created).IsFalse();
    }

    /// <summary>
    /// Verifies generation does not return IDs reserved by routing.
    /// </summary>
    [Test]
    public async Task GenerateNewId_WhenOnlyReservedSingleCharacterRemains_UsesNextLength()
    {
        await using var dbContext = CreateDbContext();

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var c in IdAlphabet.Where(x => x != 'Q'))
        {
            dbContext.Redirects.Add(new StoredUrlRedirect
            {
                Id = c.ToString(),
                Target = "https://example.test",
                ExpiresAtUtc = now + 3600
            });
        }

        await dbContext.SaveChangesAsync();

        var store = CreateStore(dbContext, reservedIdProvider: new TestReservedIdProvider(["q"]));
        var id = await store.GenerateNewId();

        await Assert.That(id.Length).IsEqualTo(2);
    }

    /// <summary>
    /// Verifies manual deletion removes any cooldown block and makes the ID reusable immediately.
    /// </summary>
    [Test]
    public async Task DeleteAsync_RemovesCooldownBlockAndAllowsImmediateReuse()
    {
        await using var dbContext = CreateDbContext();
        var store = CreateStore(dbContext, new SqliteOptions(ExpiredIdReuseBlockSeconds: 3600));

        var created = await store.CreateAsync("abc", "https://example.test", DateTime.UtcNow.AddMinutes(5));
        await Assert.That(created).IsTrue();

        await store.DeleteAsync("abc");

        var recreated = await store.CreateAsync("abc", "https://other.example", DateTime.UtcNow.AddHours(1));

        await Assert.That(recreated).IsTrue();
        await Assert.That(await dbContext.BlockedRedirectIds.AnyAsync(x => x.Id == "abc")).IsTrue();
    }

    private static SqliteUrlStore CreateStore(
        JustShortItDbContext dbContext,
        SqliteOptions? sqliteOptions = null,
        IReservedIdProvider? reservedIdProvider = null)
    {
        return new SqliteUrlStore(
            dbContext,
            sqliteOptions ?? DefaultSqliteOptions,
            reservedIdProvider ?? EmptyReservedIdProvider,
            NullLogger<SqliteUrlStore>.Instance);
    }

    /// <summary>
    /// Creates an isolated in-memory EF context for each test to avoid cross-test state leakage.
    /// </summary>
    /// <returns>A fresh <see cref="JustShortItDbContext"/> backed by a unique in-memory database name.</returns>
    private static JustShortItDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JustShortItDbContext>()
            .UseInMemoryDatabase($"jsi-tests-{Guid.NewGuid()}")
            .Options;

        return new JustShortItDbContext(options);
    }

    private sealed class TestReservedIdProvider : IReservedIdProvider
    {
        public TestReservedIdProvider(IEnumerable<string> reservedIds)
        {
            ReservedIds = new HashSet<string>(reservedIds, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlySet<string> ReservedIds { get; }
    }
}
