using JustShortIt.Model;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class SqliteUrlStore
{
    private const string IdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int MaxIdLength = 16;

    private readonly JustShortItDbContext _dbContext;
    private readonly ILogger<SqliteUrlStore> _logger;
    private readonly IReservedIdProvider _reservedIdProvider;
    private readonly SqliteOptions _sqliteOptions;

    /// <summary>
    /// Creates a URL store backed by the provided EF Core context.
    /// </summary>
    /// <param name="dbContext">Database context used for redirect queries and updates.</param>
    /// <param name="sqliteOptions">SQLite-specific options for the URL store. Those contain configuration settings regarding database behavior.</param>
    /// <param name="reservedIdProvider">Provider exposing route-reserved IDs that should never be assigned to redirects.</param>
    /// <param name="logger">Logger used to record redirect creation, deletion, and generation events.</param>
    public SqliteUrlStore(
        JustShortItDbContext dbContext,
        SqliteOptions sqliteOptions,
        IReservedIdProvider reservedIdProvider,
        ILogger<SqliteUrlStore> logger)
    {
        _dbContext = dbContext;
        _sqliteOptions = sqliteOptions;
        _reservedIdProvider = reservedIdProvider;
        _logger = logger;
    }

    /// <summary>
    /// Resolves an active redirect target for the specified ID.
    /// </summary>
    /// <param name="id">Short ID to resolve.</param>
    /// <returns>
    /// The target URL when the ID exists and has not expired; otherwise <see langword="null"/>.
    /// </returns>
    public async Task<string?> GetTargetAsync(string id)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await _dbContext.Redirects
            .AsNoTracking()
            .Where(x => x.Id == id && x.ExpiresAtUtc > now)
            .Select(x => x.Target)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Determines whether the specified ID currently maps to an unexpired redirect.
    /// </summary>
    /// <param name="id">Short ID to check.</param>
    /// <returns><see langword="true"/> when an active redirect exists; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ExistsAsync(string id)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await _dbContext.Redirects
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.ExpiresAtUtc > now);
    }

    /// <summary>
    /// Creates or refreshes a redirect mapping for an ID.
    /// </summary>
    /// <param name="id">Short ID to create or replace.</param>
    /// <param name="target">Destination URL for the redirect.</param>
    /// <param name="expirationUtc">Expiration timestamp expected to be in UTC.</param>
    /// <returns>
    /// <see langword="true"/> when the mapping is written; <see langword="false"/> when the ID is already held by
    /// another active redirect or a concurrent write wins the race.
    /// </returns>
    /// <remarks>
    /// If the ID exists but is expired, the existing row is updated in place rather than creating a second row.
    /// </remarks>
    public async Task<bool> CreateAsync(string id, string target, DateTime expirationUtc)
    {
        if (_reservedIdProvider.ReservedIds.Contains(id))
        {
            _logger.LogWarning("Create redirect rejected because ID {RedirectId} is reserved by application routing.", id);
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiration = new DateTimeOffset(expirationUtc).ToUnixTimeSeconds();

        var existingRedirect = await _dbContext.Redirects.SingleOrDefaultAsync(x => x.Id == id);
        if (existingRedirect is not null && existingRedirect.ExpiresAtUtc > now)
        {
            _logger.LogWarning("Create redirect rejected because ID {RedirectId} is already active.", id);
            return false;
        }

        var existingBlock = await _dbContext.BlockedRedirectIds.SingleOrDefaultAsync(x => x.Id == id);
        if (existingBlock is not null && existingBlock.ExpiresAtUtc > now)
        {
            _logger.LogWarning("Create redirect rejected because ID {RedirectId} is still blocked until {BlockedUntilUtc}.", id, existingBlock.ExpiresAtUtc);
            return false;
        }

        if (existingRedirect is not null)
        {
            existingRedirect.Target = target;
            existingRedirect.ExpiresAtUtc = expiration;
        }
        else
        {
            _dbContext.Redirects.Add(new StoredUrlRedirect
            {
                Id = id,
                Target = target,
                ExpiresAtUtc = expiration
            });
        }

        var blockExpiration = expiration + _sqliteOptions.ExpiredIdReuseBlockSeconds;
        if (existingBlock is not null)
        {
            existingBlock.ExpiresAtUtc = blockExpiration;
        }
        else
        {
            _dbContext.BlockedRedirectIds.Add(new BlockedRedirectId
            {
                Id = id,
                ExpiresAtUtc = blockExpiration
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            
            // ReSharper disable once InvertIf
            if (_logger.IsEnabled(LogLevel.Information))
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (existingRedirect is null)
                {
                    _logger.LogInformation("Created redirect {RedirectId}.", id);
                }
                else
                {
                    _logger.LogInformation("Refreshed expired redirect {RedirectId}.", id);
                }
            }

            return true;
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Create redirect race detected for ID {RedirectId}; operation aborted.", id);
            return false;
        }
    }

    /// <summary>
    /// Deletes a redirect mapping if it exists.
    /// </summary>
    /// <param name="id">Short ID to delete.</param>
    /// <remarks>
    /// This operation is idempotent; missing IDs are treated as a no-op.
    /// </remarks>
    public async Task DeleteAsync(string id)
    {
        var existingRedirect = await _dbContext.Redirects.SingleOrDefaultAsync(x => x.Id == id);
        var existingBlock = await _dbContext.BlockedRedirectIds.SingleOrDefaultAsync(x => x.Id == id);

        if (existingRedirect is null && existingBlock is null)
        {
            _logger.LogDebug("Delete requested for missing redirect {RedirectId}; no action taken.", id);
            return;
        }

        if (existingRedirect is not null)
        {
            _dbContext.Redirects.Remove(existingRedirect);
        }

        if (existingBlock is not null)
        {
            _dbContext.BlockedRedirectIds.Remove(existingBlock);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Deleted redirect {RedirectId}.", id);
    }

    /// <summary>
    /// Generates a short ID that is currently unused by active redirects.
    /// </summary>
    /// <returns>A unique ID composed of characters from the internal alphabet.</returns>
    /// <remarks>
    /// IDs are generated with increasing length. The generator prefers the shortest available length and ignores
    /// expired IDs when checking uniqueness.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no ID can be generated up to the configured max length.</exception>
    public async Task<string> GenerateNewId()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        for (var length = 1; length <= MaxIdLength; length++)
        {
            var currentLength = length;

            var unavailableIds = await _dbContext.Redirects
                .AsNoTracking()
                .Where(x => x.ExpiresAtUtc > now && x.Id.Length == currentLength)
                .Select(x => x.Id)
                .ToHashSetAsync();

            var blockedIds = await _dbContext.BlockedRedirectIds
                .AsNoTracking()
                .Where(x => x.ExpiresAtUtc > now && x.Id.Length == currentLength)
                .Select(x => x.Id)
                .ToHashSetAsync();

            unavailableIds.UnionWith(blockedIds);

            foreach (var reservedId in _reservedIdProvider.ReservedIds)
            {
                if (reservedId.Length == currentLength)
                {
                    unavailableIds.UnionWith(ExpandReservedIdCandidates(reservedId));
                }
            }

            // If a length is fully saturated, move on to the next one.
            if (unavailableIds.Count >= Math.Pow(IdAlphabet.Length, currentLength))
            {
                continue;
            }

            while (true)
            {
                var candidate = GenerateCandidate(currentLength);

                if (!unavailableIds.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new InvalidOperationException("Unable to generate a unique ID.");
    }

    /// <summary>
    /// Generates a random candidate ID for the requested length.
    /// </summary>
    /// <param name="length">Number of characters to include in the candidate ID.</param>
    /// <returns>A random string drawn from the allowed ID alphabet.</returns>
    private static string GenerateCandidate(int length)
    {
        var chars = new char[length];

        for (var index = 0; index < length; index++)
        {
            chars[index] = IdAlphabet[Random.Shared.Next(IdAlphabet.Length)];
        }

        return new string(chars);
    }

    private static IEnumerable<string> ExpandReservedIdCandidates(string reservedId)
    {
        var candidates = new List<string> { string.Empty };

        foreach (var nextCharacters in reservedId.Select(GetEquivalentAlphabetCharacters))
        {
            if (nextCharacters.Count == 0)
            {
                return [];
            }

            var nextCandidates = new List<string>(candidates.Count * nextCharacters.Count);
            nextCandidates.AddRange(from prefix in candidates from nextCharacter in nextCharacters select prefix + nextCharacter);

            candidates = nextCandidates;
        }

        return candidates;
    }

    private static IReadOnlyList<char> GetEquivalentAlphabetCharacters(char character)
    {
        var matches = new HashSet<char>();

        if (IdAlphabet.Contains(character))
        {
            matches.Add(character);
        }

        var uppercase = char.ToUpperInvariant(character);
        if (IdAlphabet.Contains(uppercase))
        {
            matches.Add(uppercase);
        }

        var lowercase = char.ToLowerInvariant(character);
        if (IdAlphabet.Contains(lowercase))
        {
            matches.Add(lowercase);
        }

        return matches.ToArray();
    }
}