namespace JustShortIt.Model;

/// <summary>
/// Configures SQLite persistence settings for the application.
/// </summary>
/// <param name="Path">
/// Relative or absolute path to the SQLite database file. Relative paths are resolved from the application base directory.
/// </param>
/// <param name="ExpiredIdReuseBlockSeconds">
/// Number of seconds an ID remains unavailable after its redirect expires naturally.
/// </param>
public record SqliteOptions(string Path = "data/justshortit.db", long ExpiredIdReuseBlockSeconds = 5_184_000);