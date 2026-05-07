using System.Collections.Concurrent;
using System.Net;

namespace JustShortIt.Service;

public class LoginAttemptService
{
    private readonly ConcurrentDictionary<string, AttemptState> _attempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<LoginAttemptService> _logger;

    public LoginAttemptService(ILogger<LoginAttemptService> logger)
    {
        _logger = logger;
    }

    public bool IsLockedOut(string username, string remoteIp, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        var normalizedUsername = NormalizeUsername(username);
        var normalizedRemoteIp = NormalizeRemoteIp(remoteIp);
        var key = BuildKey(normalizedUsername, normalizedRemoteIp);

        if (!_attempts.TryGetValue(key, out var state))
        {
            return false;
        }

        lock (state.Sync)
        {
            var now = DateTimeOffset.UtcNow;
            if (state.LockedUntilUtc == DateTimeOffset.MinValue)
            {
                return false;
            }

            if (state.LockedUntilUtc <= now)
            {
                _logger.LogInformation(
                    "Login lockout expired for username {Username} from {RemoteIp}; resetting failure counter.",
                    normalizedUsername,
                    normalizedRemoteIp);

                state.ConsecutiveFailures = 0;
                state.LockedUntilUtc = DateTimeOffset.MinValue;
                return false;
            }

            remaining = state.LockedUntilUtc - now;
            _logger.LogWarning(
                "Login lockout active for username {Username} from {RemoteIp}. RemainingSeconds={RemainingSeconds}.",
                normalizedUsername,
                normalizedRemoteIp,
                Math.Ceiling(remaining.TotalSeconds));
            return true;
        }
    }

    public TimeSpan RegisterFailure(string username, string remoteIp)
    {
        var normalizedUsername = NormalizeUsername(username);
        var normalizedRemoteIp = NormalizeRemoteIp(remoteIp);
        var state = _attempts.GetOrAdd(BuildKey(normalizedUsername, normalizedRemoteIp), _ => new AttemptState());

        lock (state.Sync)
        {
            var now = DateTimeOffset.UtcNow;

            if (state.LockedUntilUtc > now)
            {
                var remainingLockout = state.LockedUntilUtc - now;
                _logger.LogWarning(
                    "Login failure received while lockout is active for username {Username} from {RemoteIp}. RemainingSeconds={RemainingSeconds}.",
                    normalizedUsername,
                    normalizedRemoteIp,
                    Math.Ceiling(remainingLockout.TotalSeconds));
                return remainingLockout;
            }

            state.ConsecutiveFailures++;

            var exponent = Math.Min(state.ConsecutiveFailures - 1, 5);
            var delay = TimeSpan.FromSeconds(Math.Pow(2, exponent));

            if (state.ConsecutiveFailures >= 5)
            {
                state.LockedUntilUtc = now.AddMinutes(5);
                delay = state.LockedUntilUtc - now;
                _logger.LogWarning(
                    "Login lockout triggered for username {Username} from {RemoteIp}. Failures={FailureCount}, LockedUntilUtc={LockedUntilUtc}.",
                    normalizedUsername,
                    normalizedRemoteIp,
                    state.ConsecutiveFailures,
                    state.LockedUntilUtc);
            }
            else
            {
                _logger.LogWarning(
                    "Login failure recorded for username {Username} from {RemoteIp}. Failures={FailureCount}, BackoffSeconds={BackoffSeconds}.",
                    normalizedUsername,
                    normalizedRemoteIp,
                    state.ConsecutiveFailures,
                    Math.Ceiling(delay.TotalSeconds));
            }

            return delay;
        }
    }

    public void RegisterSuccess(string username, string remoteIp)
    {
        var normalizedUsername = NormalizeUsername(username);
        var normalizedRemoteIp = NormalizeRemoteIp(remoteIp);

        if (_attempts.TryRemove(BuildKey(normalizedUsername, normalizedRemoteIp), out var state))
        {
            _logger.LogInformation(
                "Login attempt state cleared after successful authentication for username {Username} from {RemoteIp}. PreviousFailures={FailureCount}.",
                normalizedUsername,
                normalizedRemoteIp,
                state.ConsecutiveFailures);
        }
    }

    private string NormalizeRemoteIp(string remoteIp)
    {
        if (string.IsNullOrWhiteSpace(remoteIp))
        {
            _logger.LogWarning(
                "Remote IP is missing while evaluating login attempts. If running behind a reverse proxy, verify forwarded headers middleware/order and trusted proxy config.");
            return "unknown";
        }

        var candidate = remoteIp.Trim();
        if (candidate.Contains(','))
        {
            var firstValue = candidate.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstValue))
            {
                _logger.LogInformation(
                    "Remote IP input contained multiple values; using first entry {RemoteIp}. RawValue={RawValue}.",
                    firstValue,
                    candidate);
                candidate = firstValue;
            }
        }

        if (!IPAddress.TryParse(candidate, out var parsedAddress))
        {
            _logger.LogWarning(
                "Remote IP value {RemoteIp} is not a valid IP address. This may indicate reverse proxy forwarding misconfiguration.",
                candidate);
            return "unknown";
        }

        return parsedAddress.IsIPv4MappedToIPv6
            ? parsedAddress.MapToIPv4().ToString()
            : parsedAddress.ToString();
    }

    private static string NormalizeUsername(string username) =>
        string.IsNullOrWhiteSpace(username) ? "[empty-username]" : username.Trim();

    private static string BuildKey(string username, string remoteIp) =>
        string.Concat(username, "|", remoteIp);

    private sealed class AttemptState
    {
        public Lock Sync { get; } = new();
        public int ConsecutiveFailures { get; set; }
        public DateTimeOffset LockedUntilUtc { get; set; } = DateTimeOffset.MinValue;
    }
}