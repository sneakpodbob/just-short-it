using JustShortIt.Model.Dto;

namespace JustShortIt.Service; 

public class AuthenticationService
{
    private ConfiguredAccount Account { get; }

    /// <summary>
    /// Creates an authentication helper backed by a single configured user credential.
    /// </summary>
    /// <param name="account">The configured account that all login checks are validated against.</param>
    public AuthenticationService(ConfiguredAccount account)
    {
        Account = account;
    }

    /// <summary>
    /// Validates whether the provided credentials match the configured application user.
    /// </summary>
    /// <param name="username">Username to validate. Comparison is case-insensitive using current culture rules.</param>
    /// <param name="password">Raw password provided by the login form.</param>
    /// <returns><see langword="true"/> when both username and password match; otherwise <see langword="false"/>.</returns>
    public bool IsUser(string username, string password)
    {
        if (!string.Equals(Account.Username, username, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var saltedPassword = string.Concat(password, Account.PasswordSalt);
        return BCrypt.Net.BCrypt.Verify(saltedPassword, Account.PasswordHash);
    }
}