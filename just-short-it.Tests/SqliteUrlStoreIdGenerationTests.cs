using JustShortIt.Model;
using JustShortIt.Service;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Tests;

public class SqliteUrlStoreIdGenerationTests
{
    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    [Test]
    public async Task GenerateNewId_OnEmptyStore_ReturnsSingleCharacterId()
    {
        await using var dbContext = CreateDbContext();
        var store = new SqliteUrlStore(dbContext);

        var id = await store.GenerateNewId();

        await Assert.That(id.Length).IsEqualTo(1);
        await Assert.That(IdAlphabet.Contains(id[0])).IsTrue();
    }

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

        var store = new SqliteUrlStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id).IsEqualTo("Z");
    }

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

        var store = new SqliteUrlStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id.Length).IsEqualTo(2);
        await Assert.That(id.All(c => IdAlphabet.Contains(c))).IsTrue();
    }

    [Test]
    public async Task GenerateNewId_ExpiredIdsDoNotBlockCandidateReuse()
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

        await dbContext.SaveChangesAsync();

        var store = new SqliteUrlStore(dbContext);
        var id = await store.GenerateNewId();

        await Assert.That(id).IsEqualTo("Q");
    }

    private static JustShortItDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JustShortItDbContext>()
            .UseInMemoryDatabase($"jsi-tests-{Guid.NewGuid()}")
            .Options;

        return new JustShortItDbContext(options);
    }
}
