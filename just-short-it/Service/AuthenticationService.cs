using JustShortIt.Model;

namespace JustShortIt.Service; 

public class AuthenticationService
{
    private User User { get; }

    /// <summary>
    /// Creates an authentication helper backed by a single configured user credential.
    /// </summary>
    /// <param name="user">The configured user that all login checks are validated against.</param>
    public AuthenticationService(User user)
    {
        User = user;
    }

    /// <summary>
    /// Validates whether the provided credentials match the configured application user.
    /// </summary>
    /// <param name="username">Username to validate. Comparison is case-insensitive using current culture rules.</param>
    /// <param name="password">Raw password to compare exactly.</param>
    /// <returns><see langword="true"/> when both username and password match; otherwise <see langword="false"/>.</returns>
    public bool IsUser(string username, string password)
        => string.Equals(User.Username, username, StringComparison.CurrentCultureIgnoreCase) &&
           User.Password == password;
}